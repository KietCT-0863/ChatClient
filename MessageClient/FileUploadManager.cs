using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Message_Client
{
    public class FileUploadManager
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public event Action<int>? UploadProgressChanged;
        public event Action<string>? UploadStatusChanged;
        public event Action<string>? UploadCompleted;
        public event Action<string>? UploadError;

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

                // Timeout cho việc kết nối TCP (tăng cho file lớn)
                var connectTask = _tcpClient.ConnectAsync(serverAddress, port);
                var timeoutTask = Task.Delay(10000); // 10 giây timeout cho file lớn
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _tcpClient.Close();
                    UploadError?.Invoke("Timeout khi kết nối tới server");
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
                    UploadError?.Invoke("Server không phản hồi handshake cho file upload");
                    return false;
                }

                _isConnected = true;
                UploadStatusChanged?.Invoke($"Đã kết nối tới server {serverAddress}:{port}");

                return true;
            }
            catch (Exception ex)
            {
                UploadError?.Invoke($"Lỗi kết nối: {ex.Message}");
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

                UploadStatusChanged?.Invoke("Đã ngắt kết nối khỏi server");
            }
            catch (Exception ex)
            {
                UploadError?.Invoke($"Lỗi khi ngắt kết nối: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        public async Task<bool> UploadFileAsync(string filePath, string username)
        {
            if (!IsConnected || _stream == null)
            {
                UploadError?.Invoke("Chưa kết nối tới server");
                return false;
            }

            if (!File.Exists(filePath))
            {
                UploadError?.Invoke("File không tồn tại");
                return false;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileName = fileInfo.Name;
                var fileSize = fileInfo.Length;

                // Kiểm tra kích thước file (giới hạn 2GB)
                if (fileSize > 2L * 1024 * 1024 * 1024)
                {
                    UploadError?.Invoke("File quá lớn (giới hạn 2GB)");
                    return false;
                }

                UploadStatusChanged?.Invoke($"Bắt đầu upload file: {fileName} ({FormatFileSize(fileSize)})");
                
                // Ước tính thời gian upload (giả sử tốc độ 10MB/s)
                var estimatedTimeSeconds = fileSize / (10 * 1024 * 1024);
                if (estimatedTimeSeconds > 60)
                {
                    var minutes = estimatedTimeSeconds / 60;
                    UploadStatusChanged?.Invoke($"Thời gian ước tính: {minutes:F1} phút");
                }
                else
                {
                    UploadStatusChanged?.Invoke($"Thời gian ước tính: {estimatedTimeSeconds:F1} giây");
                }

                // Gửi thông tin file trước
                var fileInfoMessage = $"FILE_UPLOAD_START|{username}|{fileName}|{fileSize}";
                var fileInfoBytes = Encoding.UTF8.GetBytes(fileInfoMessage);
                
                // Gửi thông tin file trực tiếp
                await _stream.WriteAsync(fileInfoBytes, 0, fileInfoBytes.Length);

                // Đọc phản hồi từ server
                var responseBuffer = new byte[1024];
                var responseBytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                var response = Encoding.UTF8.GetString(responseBuffer, 0, responseBytesRead);

                if (!response.Contains("FILE_UPLOAD_READY"))
                {
                    UploadError?.Invoke("Server không sẵn sàng nhận file");
                    return false;
                }

                // Bắt đầu upload file với buffer lớn hơn cho file 2GB
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[65536]; // 64KB buffer cho file lớn
                var totalBytesRead = 0;
                var bytesRead = 0;
                var lastProgressUpdate = 0;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await _stream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    // Cập nhật progress mỗi 1% để tránh spam UI
                    var progress = (int)((double)totalBytesRead / fileSize * 100);
                    if (progress > lastProgressUpdate)
                    {
                        UploadProgressChanged?.Invoke(progress);
                        UploadStatusChanged?.Invoke($"Đang upload... {progress}% ({FormatFileSize(totalBytesRead)}/{FormatFileSize(fileSize)})");
                        lastProgressUpdate = progress;
                    }
                }

                // Gửi tín hiệu kết thúc upload
                var endMessage = "FILE_UPLOAD_END";
                var endBytes = Encoding.UTF8.GetBytes(endMessage);
                await _stream.WriteAsync(endBytes, 0, endBytes.Length);

                // Đọc phản hồi cuối cùng
                var finalResponseBytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                var finalResponse = Encoding.UTF8.GetString(responseBuffer, 0, finalResponseBytesRead);

                if (finalResponse.Contains("FILE_UPLOAD_SUCCESS"))
                {
                    UploadCompleted?.Invoke($"Upload thành công: {fileName}");
                    UploadStatusChanged?.Invoke($"Upload hoàn thành: {fileName}");
                    return true;
                }
                else
                {
                    UploadError?.Invoke($"Server phản hồi không đúng: {finalResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                UploadError?.Invoke($"Lỗi upload file: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> PerformHandshake()
        {
            try
            {
                if (_stream == null)
                    return false;

                // Gửi tin nhắn handshake cho file upload
                const string handshakeMessage = "FILE_UPLOAD_HANDSHAKE";
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
                    return response.Contains("FILE_UPLOAD_HANDSHAKE_OK") || 
                           response.Contains("FILE_UPLOAD_HANDSHAKE");
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

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public void Dispose()
        {
            Task.Run(async () => await DisconnectAsync());
        }
    }
}
