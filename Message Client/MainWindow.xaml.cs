using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Message_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClientManager? _tcpClient;
        private readonly ObservableCollection<MessageItem> _messages;
        private readonly DispatcherTimer _timeTimer;
        private string _username = "User";

        public MainWindow()
        {
            InitializeComponent();
            
            _messages = new ObservableCollection<MessageItem>();
            lstMessages.ItemsSource = _messages;

            // Timer để cập nhật thời gian
            _timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeTimer.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
            _timeTimer.Start();

            // Hiển thị thông báo chào mừng
            AddMessage("Chào mừng bạn đến với ứng dụng chat!", "System", MessageType.System);
            AddMessage("Vui lòng nhập đầy đủ thông tin server, port và tên người dùng, sau đó nhấn 'Kết nối'.", "System", MessageType.System);
            
            // Thêm validation cho input fields
            txtServerAddress.TextChanged += ValidateConnectionInputs;
            txtServerPort.TextChanged += ValidateConnectionInputs;
            txtUsername.TextChanged += ValidateConnectionInputs;
            
            // Disable nút kết nối ban đầu
            btnConnect.IsEnabled = false;
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtServerAddress.Text))
                {
                    MessageBox.Show("Vui lòng nhập địa chỉ server", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtServerPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Vui lòng nhập port hợp lệ (1-65535)", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên người dùng", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _username = txtUsername.Text.Trim();

                // Tạo TCP client mới
                _tcpClient = new TcpClientManager();
                
                // Đăng ký các event
                _tcpClient.MessageReceived += OnMessageReceived;
                _tcpClient.ConnectionStatusChanged += OnConnectionStatusChanged;
                _tcpClient.ErrorOccurred += OnErrorOccurred;

                // Hiển thị trạng thái đang kết nối
                lblConnectionStatus.Text = "Đang kết nối...";
                lblConnectionStatus.Foreground = Brushes.Orange;
                btnConnect.IsEnabled = false;

                // Kết nối tới server
                bool connected = await _tcpClient.ConnectAsync(txtServerAddress.Text.Trim(), port);

                if (connected)
                {
                    // Cập nhật UI khi kết nối thành công
                    lblConnectionStatus.Text = $"Đã kết nối - {txtServerAddress.Text}:{port}";
                    lblConnectionStatus.Foreground = Brushes.Green;
                    
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    txtMessage.IsEnabled = true;
                    btnSend.IsEnabled = true;
                    
                    // Disable connection settings
                    txtServerAddress.IsEnabled = false;
                    txtServerPort.IsEnabled = false;
                    txtUsername.IsEnabled = false;

                    lblStatus.Text = $"Đã kết nối với server thành công - Tên: {_username}";
                    
                    AddMessage($"Đã kết nối tới server {txtServerAddress.Text}:{port}", "System", MessageType.Connected);
                    
                    // Gửi tin nhắn thông báo user kết nối
                    await _tcpClient.SendMessageAsync($"[{_username}] đã tham gia phòng chat");
                }
                else
                {
                    // Kết nối thất bại
                    lblConnectionStatus.Text = "Kết nối thất bại";
                    lblConnectionStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                
                lblConnectionStatus.Text = "Kết nối thất bại";
                lblConnectionStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
            }
        }

        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tcpClient != null)
                {
                    // Gửi tin nhắn thông báo user rời khỏi chat
                    if (_tcpClient.IsConnected)
                    {
                        await _tcpClient.SendMessageAsync($"[{_username}] đã rời khỏi phòng chat");
                    }
                    
                    await _tcpClient.DisconnectAsync();
                }

                // Cập nhật UI
                UpdateUIOnDisconnect();
                AddMessage("Đã ngắt kết nối khỏi server", "System", MessageType.Disconnected);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi ngắt kết nối: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSend.IsEnabled && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            if (_tcpClient == null || !_tcpClient.IsConnected)
            {
                MessageBox.Show("Chưa kết nối tới server", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                // Gửi tin nhắn với tên người dùng
                var formattedMessage = $"[{_username}]: {message}";
                bool sent = await _tcpClient.SendMessageAsync(formattedMessage);
                
                if (sent)
                {
                    // Hiển thị tin nhắn đã gửi
                    AddMessage(message, $"{_username} (Bạn)", MessageType.Normal);
                    txtMessage.Clear();
                    txtMessage.Focus();
                }
                else
                {
                    AddMessage("Lỗi gửi tin nhắn", "System", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                AddMessage($"Lỗi gửi tin nhắn: {ex.Message}", "System", MessageType.Error);
            }
        }

        private void BtnClearMessages_Click(object sender, RoutedEventArgs e)
        {
            _messages.Clear();
            AddMessage("Danh sách tin nhắn đã được xóa", "System", MessageType.System);
        }

        private void OnMessageReceived(string message)
        {
            Dispatcher.Invoke(() => 
            {
                // Phân tích tin nhắn từ server
                if (message.StartsWith("[BROADCAST FROM SERVER]:"))
                {
                    var content = message.Substring("[BROADCAST FROM SERVER]:".Length).Trim();
                    AddMessage(content, "Server", MessageType.System);
                }
                else if (message == "MESSAGE_SENT_SUCCESS")
                {
                    // Acknowledgment từ server - không hiển thị
                    return;
                }
                else if (message.StartsWith("Server nhận được:"))
                {
                    // Legacy acknowledgment - không hiển thị
                    return;
                }
                else
                {
                    // Tin nhắn từ user khác - parse để lấy thông tin
                    ParseAndDisplayUserMessage(message);
                }
            });
        }

        private void OnConnectionStatusChanged(string message)
        {
            Dispatcher.Invoke(() => 
            {
                lblStatus.Text = message;
                AddMessage(message, "System", MessageType.System);
                
                if (message.Contains("ngắt kết nối") || message.Contains("gián đoạn"))
                {
                    UpdateUIOnDisconnect();
                }
            });
        }

        private void OnErrorOccurred(string error)
        {
            Dispatcher.Invoke(() => 
            {
                lblStatus.Text = error;
                AddMessage(error, "System", MessageType.Error);
                
                // Nếu có lỗi nghiêm trọng, cập nhật UI
                if (error.Contains("Lỗi kết nối") || error.Contains("ngắt kết nối"))
                {
                    UpdateUIOnDisconnect();
                }
            });
        }

        private void UpdateUIOnDisconnect()
        {
            lblConnectionStatus.Text = "Chưa kết nối";
            lblConnectionStatus.Foreground = Brushes.Red;
            
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            txtMessage.IsEnabled = false;
            btnSend.IsEnabled = false;
            
            // Enable connection settings
            txtServerAddress.IsEnabled = true;
            txtServerPort.IsEnabled = true;
            txtUsername.IsEnabled = true;

            lblStatus.Text = "Đã ngắt kết nối khỏi server";
        }

        private void AddMessage(string message, string sender, MessageType type)
        {
            _messages.Add(new MessageItem(message, sender, type));
            
            // Auto scroll to bottom
            if (lstMessages.Items.Count > 0)
            {
                lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
            }

            // Giới hạn số lượng tin nhắn (để tránh chiếm quá nhiều memory)
            while (_messages.Count > 500)
            {
                _messages.RemoveAt(0);
            }
        }

        private void ParseAndDisplayUserMessage(string message)
        {
            try
            {
                // Format tin nhắn từ server: "[username]: content"
                if (message.StartsWith("[") && message.Contains("]:"))
                {
                    var endBracket = message.IndexOf("]:");
                    if (endBracket > 1)
                    {
                        var senderInfo = message.Substring(1, endBracket - 1); // Bỏ dấu [
                        var content = message.Substring(endBracket + 2).Trim(); // Bỏ ]:
                        
                        // Hiển thị tin nhắn từ user khác
                        AddMessage(content, senderInfo, MessageType.Normal);
                        return;
                    }
                }
                
                // Nếu không parse được, hiển thị như tin nhắn hệ thống
                AddMessage(message, "Server", MessageType.System);
            }
            catch (Exception)
            {
                // Nếu có lỗi parse, hiển thị nguyên bản
                AddMessage(message, "Server", MessageType.System);
            }
        }

        private void ValidateConnectionInputs(object sender, EventArgs e)
        {
            var serverAddress = txtServerAddress.Text.Trim();
            var portText = txtServerPort.Text.Trim();
            var username = txtUsername.Text.Trim();

            // Reset trạng thái
            btnConnect.IsEnabled = false;
            lblStatus.Text = "Vui lòng nhập đầy đủ thông tin";

            // Validate server address
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                lblStatus.Text = "Vui lòng nhập địa chỉ server";
                return;
            }

            // Validate port
            if (string.IsNullOrWhiteSpace(portText))
            {
                lblStatus.Text = "Vui lòng nhập port";
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                lblStatus.Text = "Port phải là số từ 1 đến 65535";
                return;
            }

            // Validate username
            if (string.IsNullOrWhiteSpace(username))
            {
                lblStatus.Text = "Vui lòng nhập tên người dùng";
                return;
            }

            if (username.Length < 2)
            {
                lblStatus.Text = "Tên người dùng phải có ít nhất 2 ký tự";
                return;
            }

            if (username.Length > 20)
            {
                lblStatus.Text = "Tên người dùng không được quá 20 ký tự";
                return;
            }

            // Tất cả đều hợp lệ
            btnConnect.IsEnabled = true;
            lblStatus.Text = $"Sẵn sàng kết nối tới {serverAddress}:{port} với tên '{username}'";
        }

        protected override async void OnClosed(EventArgs e)
        {
            _timeTimer.Stop();
            
            if (_tcpClient != null)
            {
                if (_tcpClient.IsConnected)
                {
                    await _tcpClient.SendMessageAsync($"[{_username}] đã rời khỏi phòng chat");
                }
                _tcpClient.Dispose();
            }
            
            base.OnClosed(e);
        }
    }
}