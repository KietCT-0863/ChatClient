using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Message_Client
{
    public class TcpClientManager
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public event Action<string>? MessageReceived;
        public event Action<string>? ConnectionStatusChanged;
        public event Action<string>? ErrorOccurred;

        public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

        public async Task<bool> ConnectAsync(string serverAddress, int port)
        {
            try
            {
                if (_isConnected)
                {
                    await DisconnectAsync();
                }

                _tcpClient = new TcpClient();
                _cancellationTokenSource = new CancellationTokenSource();

                // Timeout cho việc kết nối TCP
                var connectTask = _tcpClient.ConnectAsync(serverAddress, port);
                var timeoutTask = Task.Delay(5000); // 5 giây timeout
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _tcpClient.Close();
                    ErrorOccurred?.Invoke("Timeout khi kết nối tới server");
                    return false;
                }

                // Chờ task connect hoàn thành và kiểm tra exception
                await connectTask;

                _stream = _tcpClient.GetStream();

                // Thực hiện handshake để xác nhận server đang hoạt động
                var handshakeSuccess = await PerformHandshake();
                
                if (!handshakeSuccess)
                {
                    _tcpClient.Close();
                    ErrorOccurred?.Invoke("Server không phản hồi - có thể server chưa khởi động");
                    return false;
                }

                _isConnected = true;
                ConnectionStatusChanged?.Invoke($"Đã kết nối tới server {serverAddress}:{port}");

                // Bắt đầu lắng nghe tin nhắn từ server
                _ = Task.Run(() => ListenForMessagesAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Lỗi kết nối: {ex.Message}");
                _isConnected = false;
                
                // Cleanup nếu có lỗi
                try
                {
                    _tcpClient?.Close();
                }
                catch { }
                
                return false;
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource?.Cancel();

                _stream?.Close();
                _tcpClient?.Close();

                _stream?.Dispose();
                _tcpClient?.Dispose();
                _cancellationTokenSource?.Dispose();

                _stream = null;
                _tcpClient = null;
                _cancellationTokenSource = null;

                ConnectionStatusChanged?.Invoke("Đã ngắt kết nối khỏi server");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Lỗi khi ngắt kết nối: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        public async Task<bool> SendMessageAsync(string message)
        {
            if (!IsConnected || _stream == null)
            {
                ErrorOccurred?.Invoke("Chưa kết nối tới server");
                return false;
            }

            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Lỗi gửi tin nhắn: {ex.Message}");
                return false;
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested && _stream != null)
                {
                    try
                    {
                        var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead == 0)
                        {
                            // Server đã đóng kết nối
                            ConnectionStatusChanged?.Invoke("Server đã ngắt kết nối");
                            break;
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(message);
                    }
                    catch (OperationCanceledException)
                    {
                        // Bị hủy bởi token
                        break;
                    }
                    catch (IOException)
                    {
                        // Kết nối bị đóng
                        ConnectionStatusChanged?.Invoke("Kết nối bị gián đoạn");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ErrorOccurred?.Invoke($"Lỗi khi lắng nghe tin nhắn: {ex.Message}");
                }
            }
            finally
            {
                _isConnected = false;
            }
        }

        private async Task<bool> PerformHandshake()
        {
            try
            {
                if (_stream == null)
                    return false;

                // Gửi tin nhắn handshake
                const string handshakeMessage = "HANDSHAKE_REQUEST";
                var messageBytes = Encoding.UTF8.GetBytes(handshakeMessage);
                
                // Timeout cho handshake
                using var handshakeTimeout = new CancellationTokenSource(3000); // 3 giây
                
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length, handshakeTimeout.Token);

                // Đọc phản hồi từ server
                var buffer = new byte[1024];
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, handshakeTimeout.Token);
                
                if (bytesRead > 0)
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Kiểm tra server có phản hồi đúng không
                    return response.Contains("Server nhận được: HANDSHAKE_REQUEST") || 
                           response.Contains("HANDSHAKE_REQUEST");
                }
                
                return false;
            }
            catch (OperationCanceledException)
            {
                // Timeout - server không phản hồi
                return false;
            }
            catch (Exception)
            {
                // Bất kỳ lỗi nào khác cũng coi như handshake thất bại
                return false;
            }
        }

        public NetworkStream? GetStream()
        {
            return _stream;
        }

        public void Dispose()
        {
            Task.Run(async () => await DisconnectAsync());
        }
    }
}