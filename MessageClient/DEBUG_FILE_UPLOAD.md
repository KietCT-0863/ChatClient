# Debug File Upload Issue

## Vấn đề đã sửa

### 1. **Protocol Mismatch**
- **Trước**: Client gửi độ dài message trước khi gửi message
- **Sau**: Client gửi message trực tiếp
- **Lý do**: Server không đọc độ dài message, chỉ đọc message trực tiếp

### 2. **Server Processing**
- **Trước**: Server không đợi HandleFileUploadAsync hoàn thành
- **Sau**: Server đợi xử lý upload hoàn thành và break khỏi vòng lặp
- **Lý do**: Tránh đọc thêm data trong khi đang xử lý file upload

## Cách test

### 1. **Khởi động Server**
```bash
cd "D:\FPT\KI_7\PRN222\Project\MessageServer\MessageServer"
dotnet run
```

### 2. **Khởi động Client**
```bash
cd "D:\FPT\KI_7\PRN222\Project\MessageClient\MessageClient"
dotnet run
```

### 3. **Test Upload**
1. Kết nối client tới server
2. Chọn file nhỏ (< 1MB) để test
3. Nhấn Upload
4. Kiểm tra progress bar và status

## Debug Steps

### Nếu vẫn gặp lỗi "Server không sẵn sàng nhận file":

1. **Kiểm tra Server Log**
   - Xem có thông báo "Bắt đầu upload file" không
   - Xem có lỗi gì trong server console không

2. **Kiểm tra Client Log**
   - Xem có thông báo "Bắt đầu upload file" không
   - Xem có lỗi kết nối không

3. **Kiểm tra Network**
   - Đảm bảo server và client cùng port
   - Kiểm tra firewall settings

4. **Test với file nhỏ**
   - Thử với file text nhỏ (< 1KB)
   - Kiểm tra có upload thành công không

## Protocol Flow

### Client → Server:
1. `FILE_UPLOAD_HANDSHAKE` → `FILE_UPLOAD_HANDSHAKE_OK`
2. `FILE_UPLOAD_START|username|filename|filesize` → `FILE_UPLOAD_READY`
3. File data (8KB chunks)
4. `FILE_UPLOAD_END` → `FILE_UPLOAD_SUCCESS`

### Expected Server Responses:
- `FILE_UPLOAD_HANDSHAKE_OK` - Handshake thành công
- `FILE_UPLOAD_READY` - Server sẵn sàng nhận file
- `FILE_UPLOAD_SUCCESS` - Upload hoàn thành

## Troubleshooting

### Lỗi "Server không sẵn sàng nhận file"
- **Nguyên nhân**: Server không gửi `FILE_UPLOAD_READY`
- **Giải pháp**: Kiểm tra server có parse đúng message không

### Lỗi "Timeout khi kết nối"
- **Nguyên nhân**: Server không phản hồi handshake
- **Giải pháp**: Kiểm tra server có đang chạy không

### Lỗi "File quá lớn"
- **Nguyên nhân**: File > 100MB
- **Giải pháp**: Chọn file nhỏ hơn để test

## Test Files

### File nhỏ để test:
- Tạo file text với nội dung: "Hello World"
- Lưu thành `test.txt`
- Upload file này để test

### File trung bình:
- File ảnh JPG (~100KB)
- File PDF nhỏ (~500KB)

## Log Messages

### Server sẽ hiển thị:
- "Bắt đầu upload file từ username: filename (size)"
- "Upload thành công từ username: filename -> timestamp_username_filename"

### Client sẽ hiển thị:
- "Bắt đầu upload file: filename (size)"
- "Đang upload... X% (sent/total)"
- "Upload thành công: filename"
