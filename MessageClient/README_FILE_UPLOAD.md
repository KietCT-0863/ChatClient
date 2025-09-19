# Tính năng Upload File Song Song với Chat

## Mô tả
Đã thêm tính năng upload file vào ứng dụng MessageClient và MessageServer, cho phép người dùng upload file lên server trong khi vẫn có thể chat bình thường (xử lý parallel).

## Các thay đổi đã thực hiện

### 1. MessageClient - Giao diện Upload File

#### MainWindow.xaml
- **Thêm GroupBox Upload File**: Giao diện upload file với các thành phần:
  - TextBox hiển thị đường dẫn file
  - Nút "Chọn file" để browse file
  - Nút "Upload" để bắt đầu upload
  - ProgressBar hiển thị tiến trình upload
  - TextBlock hiển thị trạng thái upload
- **Cập nhật Grid Layout**: Thêm RowDefinition cho phần upload file

#### MainWindow.xaml.cs
- **Thêm FileUploadManager**: Quản lý kết nối và upload file
- **BtnBrowseFile_Click()**: Xử lý chọn file với OpenFileDialog
- **BtnUploadFile_Click()**: Xử lý upload file song song với chat
- **Event Handlers**: Xử lý progress, status, completion và error
- **Enable/Disable Logic**: Cập nhật trạng thái các nút khi kết nối/ngắt kết nối

### 2. FileUploadManager.cs - Quản lý Upload

#### Tính năng chính:
- **Kết nối riêng biệt**: Tạo kết nối TCP riêng cho upload file
- **Handshake riêng**: Protocol handshake cho file upload
- **Upload song song**: Hoạt động độc lập với chat
- **Progress tracking**: Theo dõi tiến trình upload real-time
- **Error handling**: Xử lý lỗi và timeout

#### Protocol Upload:
1. **FILE_UPLOAD_HANDSHAKE**: Xác nhận kết nối
2. **FILE_UPLOAD_START|username|filename|filesize**: Gửi thông tin file
3. **FILE_UPLOAD_READY**: Server sẵn sàng nhận file
4. **File Data**: Gửi dữ liệu file theo chunks 8KB
5. **FILE_UPLOAD_END**: Tín hiệu kết thúc
6. **FILE_UPLOAD_SUCCESS**: Xác nhận thành công

### 3. MessageServer - Xử lý File Upload

#### TcpServer.cs
- **Thêm Events**: FileUploadStarted, FileUploadCompleted, FileUploadError
- **HandleFileUploadAsync()**: Xử lý upload file từ client
- **File Storage**: Lưu file vào thư mục UploadedFiles với tên unique
- **Broadcast**: Thông báo upload file tới tất cả clients
- **Validation**: Kiểm tra kích thước file (giới hạn 100MB)

#### MainWindow.xaml.cs
- **Event Handlers**: Xử lý các event upload file
- **Logging**: Hiển thị thông tin upload trong chat log

### 4. File Storage trên Server

#### Thư mục UploadedFiles
- **Vị trí**: `MessageServer/MessageServer/UploadedFiles/`
- **Naming Convention**: `YYYYMMDD_HHMMSS_username_filename`
- **Unique Names**: Tránh trùng lặp file
- **Auto Create**: Tự động tạo thư mục nếu chưa có

## Cách sử dụng

### 1. Khởi động Server
- Mở MessageServer
- Nhập tên server và port
- Nhấn "Start Server"

### 2. Kết nối Client
- Mở MessageClient
- Nhập địa chỉ server, port và tên người dùng
- Nhấn "Kết nối"

### 3. Upload File
- Nhấn "Chọn file" để browse file
- Chọn file muốn upload (giới hạn 100MB)
- Nhấn "Upload" để bắt đầu upload
- Theo dõi progress bar và trạng thái

### 4. Chat song song
- Trong khi upload file, vẫn có thể chat bình thường
- Upload và chat hoạt động độc lập
- Server sẽ thông báo khi có file được upload

## Tính năng nổi bật

### 🔄 **Parallel Processing**
- Upload file và chat hoạt động song song
- Không ảnh hưởng đến trải nghiệm chat
- Sử dụng kết nối TCP riêng biệt

### 📊 **Real-time Progress**
- Progress bar hiển thị tiến trình upload
- Thông báo trạng thái chi tiết
- Format file size (B, KB, MB, GB)

### 🛡️ **Error Handling**
- Xử lý lỗi kết nối và timeout
- Validation kích thước file
- Rollback khi upload thất bại

### 🔒 **Security**
- Giới hạn kích thước file (100MB)
- Tên file unique để tránh conflict
- Validation thông tin file

### 📢 **Broadcast**
- Thông báo upload file tới tất cả clients
- Hiển thị thông tin file và người upload
- Logging đầy đủ trên server

## Lưu ý kỹ thuật

### Protocol Design
- Sử dụng protocol riêng cho file upload
- Handshake để xác nhận kết nối
- Chunk-based transfer (8KB chunks)
- End-to-end validation

### Performance
- Buffer size 8KB cho optimal performance
- Async/await pattern cho non-blocking
- CancellationToken cho graceful shutdown
- Memory efficient streaming

### File Management
- Auto-create upload directory
- Unique filename generation
- Cleanup incomplete uploads
- Size validation và limits

## Troubleshooting

### Upload không thành công
1. Kiểm tra kết nối mạng
2. Kiểm tra kích thước file (< 100MB)
3. Kiểm tra quyền ghi thư mục server
4. Xem log lỗi trên server

### Progress bar không cập nhật
1. Kiểm tra kết nối upload
2. Restart client nếu cần
3. Kiểm tra firewall settings

### File không xuất hiện trên server
1. Kiểm tra thư mục UploadedFiles
2. Xem log server để debug
3. Kiểm tra quyền file system
