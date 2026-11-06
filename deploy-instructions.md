# Triển khai Library Management API trên Local IIS

Tài liệu này chỉ tập trung vào triển khai và kiểm tra ứng dụng trên IIS. Các
test case nghiệp vụ chi tiết nằm trong
[TEST-CASES.md](TEST-CASES.md).

## 1. Yêu cầu

Máy Windows cần có:

- .NET 8 SDK.
- SQL Server Express/Developer hoặc SQL Server LocalDB.
- IIS.
- .NET 8 ASP.NET Core Hosting Bundle.
- PowerShell.

Kiểm tra:

```powershell
dotnet --version
```

## 2. Chuẩn bị source và database

```powershell
git clone <URL_GITHUB_CUA_REPOSITORY> D:\Code\2610_Backend_QLMuonTraThuVien
cd D:\Code\2610_Backend_QLMuonTraThuVien
dotnet restore .\src\LibraryAPI\LibraryAPI.csproj
```

Kiểm tra connection string trong
[appsettings.json](src/LibraryAPI/appsettings.json). Với SQL Server Windows
Authentication, cấu hình mẫu là:

```text
Server=localhost;Database=LibraryManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

Nếu dùng SQL Server Express, có thể cần:

```text
Server=localhost\SQLEXPRESS;Database=LibraryManagementDb;Trusted_Connection=True;TrustServerCertificate=True
```

Tạo/cập nhật schema:

```powershell
dotnet tool update --global dotnet-ef
dotnet ef database update `
  --project .\src\LibraryAPI\LibraryAPI.csproj `
  --startup-project .\src\LibraryAPI\LibraryAPI.csproj
```

Nếu chưa cài `dotnet-ef`:

```powershell
dotnet tool install --global dotnet-ef
```

Khi API khởi động, [DatabaseInitializer.cs](src/LibraryAPI/Data/DatabaseInitializer.cs)
sẽ áp dụng migration và tự cài/cập nhật
[sp_BorrowBook.sql](database/sp_BorrowBook.sql).

Kiểm tra bằng SSMS:

```sql
USE LibraryManagementDb;
GO

SELECT name FROM sys.tables ORDER BY name;
SELECT name FROM sys.procedures WHERE name = N'sp_BorrowBook';
```

## 3. Kiểm tra local trước khi publish

Build và mở dashboard:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

Script sẽ build, khởi động API tại `http://localhost:5187`, kiểm tra `/` và
`/api/books`, sau đó mở trình duyệt.

Kết quả đạt:

```text
Build succeeded.
Smoke tests passed: dashboard HTTP 200, books HTTP 200.
```

Dashboard:

```text
http://localhost:5187/
```

Swagger khi chạy Development:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\LibraryAPI\LibraryAPI.csproj --urls http://localhost:5187
```

```text
http://localhost:5187/swagger
```

Sau khi local smoke test đạt, chạy các nhóm AUTH, RBAC, BOOK, COPY, BORROW,
RETURN, FINE và USER trong [TEST-CASES.md](TEST-CASES.md).

## 4. Publish Release

```powershell
dotnet publish .\src\LibraryAPI\LibraryAPI.csproj `
  -c Release `
  -o .\publish
```

Kiểm tra artifact:

```powershell
Test-Path .\publish\LibraryAPI.dll
Test-Path .\publish\web.config
Test-Path .\publish\database\sp_BorrowBook.sql
```

Cả ba lệnh phải trả về `True`.

## 5. Cấu hình IIS

### 5.1. Application Pool

Trong IIS Manager:

1. Tạo Application Pool tên `LibraryAPI`.
2. Chọn **.NET CLR Version: No Managed Code**.
3. Chọn **Managed pipeline mode: Integrated**.
4. Đặt **Identity** phù hợp với quyền truy cập SQL Server.

### 5.2. Website

1. Tạo Website hoặc Application trỏ tới thư mục `publish`.
2. Chọn binding HTTP, ví dụ port `8080`.
3. Đảm bảo `.NET 8 Hosting Bundle` đã cài.
4. File `web.config` phải nằm cùng thư mục với `LibraryAPI.dll`.

### 5.3. Quyền thư mục

Cấp cho `IIS_IUSRS`:

- `Read & Execute` trên thư mục `publish`.
- `Modify` trên thư mục `publish\logs`.

Không đưa password SQL hoặc JWT production vào Git. Dùng biến môi trường,
IIS configuration hoặc secret manager cho production.

## 6. Kiểm tra sau khi chạy IIS

Thay `<port>` bằng port đã cấu hình:

```powershell
$base = "http://localhost:<port>"
Invoke-WebRequest "$base/" -UseBasicParsing
Invoke-RestMethod "$base/api/books"
```

Kết quả đạt:

- `/` trả HTTP 200 và hiển thị dashboard.
- `/api/books` trả HTTP 200.
- Đăng ký/đăng nhập trả JWT.
- RBAC trả đúng 401/403.
- Mượn gọi được `sp_BorrowBook`.
- Trả sách cập nhật bản sao và phạt đúng.
- Librarian/Admin thanh toán được FineRecord.
- Log xuất hiện trong `publish\logs`.

Chạy lại toàn bộ test case nghiệp vụ trong
[TEST-CASES.md](TEST-CASES.md), thay URL local bằng URL IIS.

## 7. Xử lý lỗi IIS

### HTTP 500 khi khởi động

Kiểm tra:

1. Event Viewer → Windows Logs → Application.
2. .NET 8 Hosting Bundle.
3. `web.config`.
4. Connection string.
5. Quyền thư mục `publish` và `publish\logs`.

Chỉ bật tạm thời:

```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

Sau khi chẩn đoán xong phải tắt lại `stdoutLogEnabled`.

### Không kết nối được SQL Server

```powershell
Get-Service *SQL*
```

Xác minh SQL Server đang chạy, database tồn tại và tài khoản IIS có quyền
truy cập database.

### Không truy cập được dashboard

```powershell
Get-NetTCPConnection -LocalPort <port> -State Listen
```

Kiểm tra binding IIS, Windows Firewall và Application Pool.

## 8. Tiêu chí nghiệm thu IIS

Triển khai được xem là đạt khi:

1. Publish Release thành công.
2. IIS Application Pool dùng `No Managed Code`.
3. Website mở được dashboard HTTP 200.
4. Database và `sp_BorrowBook` tồn tại.
5. Login/JWT/RBAC hoạt động.
6. CRUD sách và user hoạt động.
7. Mượn/trả/phạt hoạt động.
8. Log được ghi.
9. Restart Application Pool không làm ứng dụng lỗi.
10. Các test case trong [TEST-CASES.md](TEST-CASES.md) đạt.
