# Message Client - Realtime Chat Client

Đây là một ứng dụng Chat Client được viết bằng C# sử dụng WPF và TCP Socket, kết nối tới MessageServer để chat realtime.

## 🚀 Tính năng

- **TCP Chat Client**: Kết nối tới chat server qua TCP protocol
- **Realtime Messaging**: Gửi và nhận tin nhắn realtime với validation
- **User Authentication**: Đăng nhập với tên người dùng tùy chỉnh
- **Connection Management**: Tự động phát hiện server disconnect
- **Smart Validation**: Kiểm tra input realtime trước khi kết nối
- **Professional UI**: Giao diện WPF hiện đại với message categorization
- **Auto Scroll**: Tự động cuộn xuống tin nhắn mới nhất

## 📁 Cấu trúc Project

```
MessageClient/
├── Message Client/
│   └── Message Client/         # WPF Client Application
│       ├── MainWindow.xaml     # Giao diện chính client
│       ├── MainWindow.xaml.cs  # Code-behind xử lý UI và chat logic
│       ├── TcpClient.cs        # TCP client core với handshake
│       └── MessageItem.cs      # Model cho tin nhắn hiển thị
└── README.md                   # Tài liệu hướng dẫn
```

## 🛠️ Cách sử dụng

### 1. Chuẩn bị

**Yêu cầu tiên quyết:**
- MessageServer phải đang chạy (xem `../MessageServer/README.md`)
- Biết địa chỉ IP/hostname và port của server

### 2. Khởi động Client

1. **Build và chạy project:**
   ```bash
   cd "MessageClient/Message Client/Message Client"
   dotnet build
   dotnet run
   ```

2. **Cấu hình kết nối:**
   - **Server**: Nhập IP hoặc hostname (VD: `localhost`, `192.168.1.100`)
   - **Port**: Nhập port của server (VD: `8080`, `7777`)
   - **Tên**: Nhập tên hiển thị (2-20 ký tự, VD: `Alice_2024`)

3. **Kết nối:**
   - Hệ thống sẽ validation realtime các thông tin
   - Click "Kết nối" khi tất cả thông tin hợp lệ
   - Client sẽ thực hiện handshake với server

### 3. Sử dụng Chat

- **Gửi tin nhắn**: Gõ tin nhắn và nhấn Enter hoặc click "Gửi"
- **Nhận tin nhắn**: Tin nhắn từ users khác hiển thị realtime
- **Ngắt kết nối**: Click "Ngắt kết nối" để thoát khỏi chat room

## 🎨 Giao diện và UX

### 📱 Layout Sections:

1. **🔗 Connection Panel**: Cấu hình server, port, username
2. **💬 Chat Area**: Hiển thị tin nhắn với color coding
3. **✍️ Message Input**: Gửi tin nhắn với validation
4. **📊 Status Bar**: Trạng thái kết nối và thời gian realtime

### 🎨 Message Types:

- **💙 Normal**: Tin nhắn từ users khác (background xanh nhạt)
- **💛 System**: Thông báo hệ thống (background vàng nhạt)  
- **❤️ Error**: Thông báo lỗi (background đỏ nhạt)
- **💚 Connected**: Thông báo kết nối (background xanh lá nhạt)
- **🧡 Your Messages**: Tin nhắn của bạn với prefix "(Bạn)"

## 📝 Protocol và Communication

### 🤝 Handshake Protocol:
```
Client → Server: HANDSHAKE_REQUEST
Client ← Server: Server nhận được: HANDSHAKE_REQUEST
→ Connection established ✅
```

### 💬 Message Protocol:
```
Client → Server: [username]: message content
Client ← Server: MESSAGE_SENT_SUCCESS (ACK)
Other Clients ← Server: [username]: message content (broadcast)
```

### 🔄 Server Broadcast:
```
All Clients ← Server: [BROADCAST FROM SERVER]: announcement
```

## 🎯 Scenarios sử dụng

### Scenario 1: Basic Chat
```
1. Alice kết nối: server localhost, port 8080, tên "Alice"
2. Bob kết nối: server localhost, port 8080, tên "Bob"  
3. Alice gửi: "Hello Bob!"
4. Bob nhận được: "Hello Bob!" from Alice
5. Bob trả lời: "Hi Alice, how are you?"
6. Alice nhận được tin nhắn từ Bob
```

### Scenario 2: Multi-User Chat Room
```
1. Server: "GameRoom-01" chạy trên port 7777
2. 5 users kết nối với tên khác nhau
3. Mọi người chat realtime, tin nhắn đồng bộ cho tất cả
4. Server gửi broadcast: "Game sẽ bắt đầu trong 5 phút!"
5. Tất cả users nhận được thông báo từ server
```

## 🔧 Validation Rules

### ✅ Server Address:
- Không được để trống
- Chấp nhận IP address hoặc hostname

### ✅ Port:
- Số nguyên từ 1 đến 65535
- Kiểm tra realtime khi gõ

### ✅ Username:
- Độ dài: 2-20 ký tự
- Không được để trống hoặc chỉ có khoảng trắng

### ✅ Connection:
- Timeout: 5 giây cho TCP connection
- Handshake timeout: 3 giây
- Auto-retry: Không (user phải click lại)

## 🛡️ Error Handling

### 🔴 Connection Errors:
- **Server not found**: "Timeout khi kết nối tới server"
- **Server not responding**: "Server không phản hồi - có thể server chưa khởi động"
- **Network issues**: Hiển thị lỗi kết nối cụ thể

### 🔴 Runtime Errors:
- **Connection lost**: Tự động detect và update UI
- **Message send failure**: Thông báo "Lỗi gửi tin nhắn"
- **Invalid input**: Validation realtime ngăn lỗi

### 🔄 Recovery:
- Tự động reset UI khi disconnect
- Cho phép reconnect với thông tin mới
- Clear message history khi cần thiết

## 🚀 Performance Features

### ⚡ Optimizations:
- **Async Operations**: Non-blocking UI với async/await
- **Memory Management**: Giới hạn 500 tin nhắn để tránh memory leak
- **Auto Scroll**: Smooth scrolling đến tin nhắn mới
- **Real-time Validation**: Instant feedback khi nhập thông tin

### 📱 UI Responsiveness:
- **Dispatcher Invoke**: Thread-safe UI updates
- **Background Tasks**: Network operations không block UI
- **Timeout Handling**: Tránh freeze khi server không phản hồi

## 🔍 Troubleshooting

### ❓ Kết nối thất bại:
1. Kiểm tra server có đang chạy không
2. Verify địa chỉ IP và port
3. Kiểm tra firewall/antivirus
4. Thử với `localhost` nếu cùng máy

### ❓ Tin nhắn không hiển thị:
1. Kiểm tra kết nối còn active không
2. Restart client và reconnect
3. Kiểm tra server log để debug

### ❓ UI lag hoặc freeze:
1. Restart application
2. Kiểm tra memory usage
3. Clear message history nếu quá nhiều

## 📞 Support

- **Server Setup**: Xem `../MessageServer/README.md`
- **Issues**: Tạo issue trong repository
- **Architecture**: WPF MVVM pattern với TCP client best practices
- **Compatibility**: .NET 8.0+ trên Windows
