# TÀI LIỆU MÔ TẢ VÀ KIỂM THỬ

## 1. Thông tin chung

- **Tên đề tài:** Hệ thống Quản lý Mượn Trả Thư viện
- **Địa chỉ kiểm thử local:** `http://localhost:5187`
- **Giao diện kiểm thử:** mở địa chỉ trên bằng trình duyệt
- **API documentation:** `http://localhost:5187/swagger` khi chạy Development
- **Database:** SQL Server, database `LibraryManagementDb`
- **Các vai trò:** `Admin`, `Librarian`, `Reader`

Mỗi lần kiểm thử cần ghi lại:

1. Mã test case.
2. Dữ liệu đã sử dụng.
3. HTTP status nhận được.
4. Nội dung response.
5. Dữ liệu sau kiểm thử trong SQL Server.
6. Kết luận `PASS` hoặc `FAIL`.

Trên giao diện, toàn bộ request được lưu trong menu **Request log**. Có thể dùng
log này để chụp ảnh minh chứng request, status code và response body.

## 2. Hướng dẫn sử dụng sau khi clone từ GitHub

### 2.1. Yêu cầu môi trường

Máy local cần cài:

- Windows 10/11.
- .NET 8 SDK.
- SQL Server Express/Developer hoặc SQL Server LocalDB.
- SQL Server Management Studio (SSMS), tùy chọn nhưng nên có để kiểm tra database.
- PowerShell.

Kiểm tra .NET SDK:

```powershell
dotnet --version
```

Kết quả cần là phiên bản `8.x`.

### 2.2. Clone source code

Mở PowerShell và chạy:

```powershell
cd D:\Code
git clone <URL_GITHUB_CUA_REPOSITORY> 2610_Backend_QLMuonTraThuVien
cd .\2610_Backend_QLMuonTraThuVien
```

Nếu repository đã tồn tại trên máy:

```powershell
cd D:\Code\2610_Backend_QLMuonTraThuVien
git pull
```

### 2.3. Kiểm tra connection string

Mở file [appsettings.json](src/LibraryAPI/appsettings.json) và kiểm tra:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=LibraryManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

Nếu SQL Server dùng instance khác, sửa phần `Server`, ví dụ:

```text
Server=localhost\SQLEXPRESS
```

Nếu dùng SQL Authentication, dùng connection string tương ứng và không commit
username/password production vào Git.

JWT key trong môi trường production phải được thay bằng secret ngẫu nhiên đủ
dài. Key mẫu trong `appsettings.json` chỉ dùng cho local demo.

### 2.4. Restore package và tạo database

Từ thư mục repository:

```powershell
dotnet restore .\src\LibraryAPI\LibraryAPI.csproj
dotnet tool update --global dotnet-ef
dotnet ef database update `
  --project .\src\LibraryAPI\LibraryAPI.csproj `
  --startup-project .\src\LibraryAPI\LibraryAPI.csproj
```

Nếu máy chưa cài `dotnet-ef`, chạy:

```powershell
dotnet tool install --global dotnet-ef
```

Lệnh `database update` tạo các bảng EF Core. Khi API khởi động, initializer sẽ
tự chạy migration còn thiếu và cài/cập nhật `sp_BorrowBook`.

Kiểm tra database trong SSMS:

```sql
USE LibraryManagementDb;
GO

SELECT name FROM sys.tables ORDER BY name;
SELECT name FROM sys.procedures WHERE name = N'sp_BorrowBook';
```

### 2.5. Build và chạy API lần đầu

Cách khuyến nghị là dùng script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

Script sẽ:

1. Build project.
2. Khởi động API tại `http://localhost:5187`.
3. Kiểm tra dashboard `/`.
4. Kiểm tra `GET /api/books`.
5. Mở trình duyệt tự động.

Nếu không dùng script:

```powershell
dotnet run --project .\src\LibraryAPI\LibraryAPI.csproj --urls http://localhost:5187
```

Sau đó mở:

```text
http://localhost:5187/
```

Swagger chỉ hiển thị khi ứng dụng chạy ở môi trường Development. Có thể chạy:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\LibraryAPI\LibraryAPI.csproj --urls http://localhost:5187
```

Mở Swagger tại:

```text
http://localhost:5187/swagger
```

### 2.6. Tạo dữ liệu demo đầu tiên

Trên dashboard:

1. Đăng ký một tài khoản Reader.
2. Đăng nhập Reader để kiểm tra JWT.
3. Tạo tài khoản `admin` bằng đăng ký Reader.
4. Trong SSMS, đổi role tài khoản demo:

```sql
USE LibraryManagementDb;
GO

UPDATE Users
SET Role = N'Admin'
WHERE Username = N'admin';
```

5. Đăng xuất và đăng nhập lại `admin` để nhận JWT có role Admin.
6. Tạo đầu sách trong mục **Sách & bản sao**.
7. Tạo các bản sao vật lý.
8. Tạo Librarian/Reader trong mục **Độc giả**.
9. Dùng Reader kiểm thử mượn, trả và tiền phạt.

### 2.7. Kiểm tra nhanh sau khi clone

Chạy các lệnh sau:

```powershell
dotnet build .\src\LibraryAPI\LibraryAPI.csproj
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

Kết quả đạt:

```text
Build succeeded.
Smoke tests passed: dashboard HTTP 200, books HTTP 200.
```

Nếu gặp lỗi `Failed to fetch`, kiểm tra API có đang chạy ở port `5187` không:

```powershell
Get-NetTCPConnection -LocalPort 5187 -State Listen
```

Nếu không có tiến trình lắng nghe, chạy lại
`scripts\build-test-open.ps1`.

Nếu gặp lỗi database:

1. Kiểm tra SQL Server service đang chạy.
2. Kiểm tra `Server` trong connection string.
3. Kiểm tra database `LibraryManagementDb` tồn tại.
4. Chạy lại `dotnet ef database update`.
5. Kiểm tra quyền tài khoản Windows/SQL đang dùng.

Sau khi các bước trên thành công, tiếp tục các nhóm test từ mục 3 trở đi.

## 3. Chuẩn bị môi trường

### 2.1. Build và khởi động

Mở PowerShell tại thư mục dự án:

```powershell
cd D:\Code\2610_Backend_QLMuonTraThuVien
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

Kết quả thành công:

```text
Build succeeded.
Smoke tests passed: dashboard HTTP 200, books HTTP 200.
```

Trình duyệt tự động mở trang dashboard. Nếu API đã chạy ở cổng 5187, script sẽ
dùng lại tiến trình đó.

### 2.2. Kiểm tra database

Trong SSMS, chọn database `LibraryManagementDb` và chạy:

```sql
SELECT name FROM sys.tables;
SELECT name FROM sys.procedures WHERE name = N'sp_BorrowBook';
```

Kết quả thành công phải có các bảng:

```text
Users
BookTitles
BookCopies
BorrowTickets
BorrowDetails
FineRecords
```

và stored procedure `sp_BorrowBook`.

### Kiểm tra cú pháp bắt buộc của Stored Procedure

Đây là kiểm tra code review, không chỉ là kiểm tra hành vi. Trong SSMS chạy:

```sql
EXEC sys.sp_helptext N'dbo.sp_BorrowBook';
```

Xác minh nội dung có các cấu trúc `BEGIN TRY`, `BEGIN TRAN`, `COMMIT`,
`BEGIN CATCH` và `ROLLBACK`. Có thể kiểm tra nhanh:

```sql
SELECT CASE
    WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_BorrowBook')) LIKE N'%BEGIN TRY%'
     AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_BorrowBook')) LIKE N'%BEGIN TRAN%'
     AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_BorrowBook')) LIKE N'%COMMIT%'
     AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_BorrowBook')) LIKE N'%BEGIN CATCH%'
     AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_BorrowBook')) LIKE N'%ROLLBACK%'
    THEN 'PASS' ELSE 'FAIL'
END AS StoredProcedureTransactionSyntax;
```

Kết quả phải là `PASS`.

### 2.3. Chuẩn bị tài khoản

Trên giao diện, mở mục **Xác thực & RBAC**, đăng ký tài khoản Reader:

```text
Username: reader01
Full name: Nguyen Van Reader
Password: Reader@12345
```

Sau khi đăng ký thành công, giao diện phải hiển thị role `Reader` và lưu JWT.

### Tạo tài khoản Admin cho môi trường demo

Ứng dụng không cho phép người chưa đăng nhập tự đăng ký trực tiếp role `Admin`.
Tài khoản đăng ký trên giao diện luôn có role `Reader`. Vì vậy cần thực hiện
đúng ba bước sau:

**Bước 1: Đăng ký tài khoản trên giao diện**

Trong mục **Đăng ký Reader**, nhập:

```text
Username: admin
Full name: System Administrator
Password: Admin@12345
```

Chọn **Đăng ký**. Lúc này tài khoản được tạo với role `Reader`.

**Bước 2: Đổi role của tài khoản vừa tạo trong SSMS**

Mở SSMS, chọn database `LibraryManagementDb`, chạy:

```sql
USE LibraryManagementDb;
GO

UPDATE Users
SET Role = N'Admin'
WHERE Username = N'admin';
```

Kiểm tra kết quả:

```sql
SELECT Id, Username, FullName, Role, Status
FROM Users
WHERE Username = N'admin';
```

Kết quả cần có `Role = Admin` và `Status = Active`.

**Bước 3: Đăng nhập lại trên giao diện**

Đăng xuất hoặc xóa JWT cũ, sau đó đăng nhập:

```text
Username: admin
Password: Admin@12345
```

Header phải hiển thị `System Administrator · Admin`. Khi đó có thể mở mục
**Độc giả**, tạo user, quản lý sách và kiểm thử quyền Admin.

Đây chỉ là cách tạo dữ liệu nhanh cho môi trường demo. Không cập nhật role trực
tiếp trong database production; production cần quy trình quản trị được kiểm
soát, audit và phân quyền riêng.

## 4. Kiểm thử kiến trúc phân tầng và Dependency Injection

### Mục đích

Xác minh Controller không thao tác trực tiếp với database và các service được
inject bằng Dependency Injection.

### Các bước

1. Mở source code.
2. Kiểm tra thư mục `Controllers`.
3. Kiểm tra thư mục `Services`.
4. Kiểm tra `Data/AppDbContext.cs`.
5. Kiểm tra constructor của từng controller và service.

### Kết quả thành công

- Controller chỉ nhận request, kiểm tra quyền và gọi service.
- Logic CRUD nằm trong `BookService` hoặc `UserService`.
- Logic mượn/trả nằm trong `BorrowService`.
- Database chỉ được truy cập qua `AppDbContext`.
- `Program.cs` đăng ký `IBookService`, `IBorrowService`, `IUserService`,
  `IAuthService` bằng `AddScoped`.
- Build không có lỗi.

## 5. Kiểm thử Entity Framework Core Code-First

### Mục đích

Xác minh hệ thống có đủ các thực thể và quan hệ dữ liệu.

### Các bước

Chạy:

```powershell
dotnet ef migrations list `
  --project .\src\LibraryAPI\LibraryAPI.csproj `
  --startup-project .\src\LibraryAPI\LibraryAPI.csproj
```

Trong SSMS chạy:

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
```

### Kết quả thành công

Có migration `InitialCreate` và database có 6 bảng nghiệp vụ:

1. `Users`
2. `BookTitles`
3. `BookCopies`
4. `BorrowTickets`
5. `BorrowDetails`
6. `FineRecords`

Các quan hệ cần đúng:

- Một `BookTitle` có nhiều `BookCopy`.
- Một `User` có nhiều `BorrowTicket`.
- Một `BorrowTicket` có nhiều `BorrowDetail`.
- Một `BorrowDetail` liên kết với một `BookCopy`.
- Một `BorrowTicket` có thể có nhiều `FineRecord`.

## 6. Kiểm thử đăng ký và đăng nhập

### AUTH-01 - Đăng ký hợp lệ

**Các bước:**

1. Mở **Xác thực & RBAC**.
2. Nhập username mới.
3. Nhập họ tên.
4. Nhập password tối thiểu 8 ký tự.
5. Chọn **Đăng ký**.

**Thành công khi:**

- HTTP status là `200`.
- Response có `userId`, `username`, `role`, `token`.
- Role là `Reader`.
- Header hiển thị tên và role.
- JWT xuất hiện trong `localStorage`.

### AUTH-02 - Đăng nhập đúng

**Các bước:**

1. Nhập username/password đã tồn tại.
2. Chọn **Đăng nhập**.

**Thành công khi:**

- HTTP `200`.
- Có JWT hợp lệ.
- Session hiển thị đúng user và role.

### AUTH-03 - Sai mật khẩu

**Các bước:**

1. Nhập username tồn tại.
2. Nhập password sai.
3. Chọn **Đăng nhập**.

**Thành công khi:**

- HTTP `401`.
- Không tạo token mới.
- Giao diện hiển thị thông báo đăng nhập thất bại.

### AUTH-04 - Username không tồn tại

**Thành công khi:**

- HTTP `401`.
- Không tiết lộ thông tin nhạy cảm về tài khoản.

### AUTH-05 - Username trùng

**Các bước:**

1. Đăng ký lại username đã tồn tại.

**Thành công khi:**

- HTTP `400`.
- Database không có user trùng thứ hai.

## 7. Kiểm thử FluentValidation

### VALIDATOR-CODE-01 - Kiểm tra cấu trúc Clean Code

Kiểm tra source code:

1. Mở thư mục `src/LibraryAPI/Validators`.
2. Xác minh các validator nằm trong `RequestValidators.cs`.
3. Mở `src/LibraryAPI/DTOs/Dtos.cs` và xác minh file này chỉ chứa DTO, không
   chứa class kế thừa `AbstractValidator`.
4. Mở `src/LibraryAPI/Program.cs` và xác minh có:

```csharp
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

**Kết quả PASS khi:**

- Có thư mục `Validators` riêng.
- Các validator là class public, top-level.
- Không có validator lồng trong DTO.
- FluentValidation được đăng ký bằng assembly scanning.
- Project build không lỗi.

### VALIDATOR-RUNTIME-01 - Kiểm tra validator được DI tự động

Không cần đăng ký từng validator thủ công. Gửi các request sai qua dashboard:

- Đăng ký password dưới 8 ký tự.
- Tạo sách có `TotalCopies` âm.
- Mượn danh sách `BookCopyIds` rỗng.
- Tạo bản sao với `BookTitleId` bằng 0.
- Cập nhật user với role không hợp lệ.

**Kết quả PASS khi:**

- Tất cả request nhận HTTP `400`.
- Response có lỗi validation.
- Không có dữ liệu không hợp lệ được ghi vào database.
- Không cần thêm dòng `AddScoped` riêng cho từng validator.

### VALIDATOR-REGRESSION-01 - Build và smoke test sau refactor

Chạy từ thư mục repository:

```powershell
dotnet build .\src\LibraryAPI\LibraryAPI.csproj --no-restore
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

**Kết quả đã đạt:**

```text
Build succeeded.
0 Warning(s)
0 Error(s)
Smoke tests passed:
- dashboard HTTP 200
- books HTTP 200
```

Đây là bằng chứng refactor validator không làm thay đổi behavior của API.

Thực hiện lần lượt các dữ liệu không hợp lệ:

| Dữ liệu | Kết quả đúng |
|---|---|
| Username rỗng | HTTP 400 |
| Username dưới 3 ký tự | HTTP 400 |
| Password dưới 8 ký tự | HTTP 400 |
| Full name rỗng | HTTP 400 |
| Title rỗng | HTTP 400 |
| Author rỗng | HTTP 400 |
| ISBN rỗng | HTTP 400 |
| TotalCopies âm | HTTP 400 |
| BookCopyIds rỗng | HTTP 400 |
| BookCopyIds lớn hơn 5 phần tử | HTTP 400 |
| ExpectedReturnDate ở quá khứ | HTTP 400 |

**Thành công khi:** request bị từ chối trước khi ghi dữ liệu sai vào database và
response có thông tin lỗi validation.

## 8. Kiểm thử JWT và RBAC

Chuẩn bị token của từng role. Đăng nhập rồi thực hiện các thao tác sau:

| Chức năng | Admin | Librarian | Reader | Không token |
|---|---:|---:|---:|---:|
| Tra cứu sách | 200 | 200 | 200 | 200 |
| Tạo/cập nhật/xóa sách | Cho phép | Cho phép | 403 | 401 |
| Thêm bản sao | Cho phép | Cho phép | 403 | 401 |
| Mượn sách | Cho phép | Cho phép | Cho phép | 401 |
| Trả sách | Cho phép | Cho phép | Cho phép | 401 |
| Thanh toán phạt | Cho phép | Cho phép | 403 | 401 |
| Quản lý user | Cho phép | 403 | 403 | 401 |

### Các bước kiểm tra

1. Đăng nhập Reader.
2. Mở **Kiểm thử yêu cầu**.
3. Chọn test Reader gọi Admin API.
4. Chuyển sang Admin hoặc Librarian.
5. Thử lại chức năng quản lý sách.

**Thành công khi:**

- Reader nhận `403 Forbidden` khi gọi API Admin/Librarian.
- Người không có token nhận `401 Unauthorized`.
- Admin và Librarian thao tác được với sách.
- Chỉ Admin thao tác được với user.

## 9. Kiểm thử quản lý đầu sách

### BOOK-01 - Tạo đầu sách

Trong mục **Sách & bản sao**, nhập:

```text
Title: Clean Code
Author: Robert C. Martin
ISBN: ISBN-TEST-001
TotalCopies: 3
```

Chọn **Thêm sách**.

**Thành công khi:**

- HTTP `201`.
- Có `BookTitle.Id`.
- Sách xuất hiện khi tra cứu.

### BOOK-02 - ISBN trùng

Tạo lại sách với `ISBN-TEST-001`.

**Thành công khi:**

- HTTP `400`.
- Không tạo bản ghi thứ hai.

### BOOK-03 - Tra cứu

Nhập `Clean`, tên tác giả hoặc ISBN rồi chọn **Tra cứu**.

**Thành công khi:**

- HTTP `200`.
- Kết quả có title, author, ISBN.
- Có danh sách bản sao và status.
- Từ khóa không tồn tại trả về mảng rỗng.

## 10. Kiểm thử quản lý bản sao vật lý

### COPY-01 - Tạo bản sao

Nhập `BookTitleId` hợp lệ và:

```text
Barcode: BARCODE-TEST-001
```

Chọn **Thêm bản sao**.

**Thành công khi:**

- HTTP `200`.
- Bản sao có status `Available`.
- Số bản sao thực tế tăng lên và không vượt `BookTitle.TotalCopies`.
- Bản sao hiển thị trong bảng sách.

### COPY-02 - Barcode trùng

Tạo bản sao với `BARCODE-TEST-001` lần nữa.

**Thành công khi:** HTTP `400`, không tạo bản sao thứ hai.

### COPY-03 - BookTitle không tồn tại

Nhập một `BookTitleId` không có trong database.

**Thành công khi:** HTTP `404` hoặc `400`, không có bản sao mồ côi.

### COPY-04 - Vượt số lượng bản sao khai báo

Tạo đủ số bản sao bằng `BookTitle.TotalCopies`, sau đó thử thêm thêm một bản sao.

**Thành công khi:** HTTP `400`, không vượt giới hạn và không tạo bản sao dư.

## 11. Kiểm thử stored procedure mượn sách

### Chuẩn bị

Tạo ít nhất 6 bản sao có status `Available`. Lấy ID từ bảng **Sách & bản sao**.

### BORROW-01 - Mượn thành công

1. Đăng nhập Reader.
2. Mở **Mượn & trả**.
3. Nhập một hoặc nhiều ID bản sao.
4. Chọn ngày trả dự kiến sau ngày hiện tại.
5. Chọn **Gọi SP mượn**.

**Thành công khi:**

- HTTP `200`.
- Response có `borrowTicketId`.
- Có một dòng `BorrowTickets`.
- Có các dòng `BorrowDetails` tương ứng.
- Các bản sao chuyển từ `Available` sang `Borrowed`.
- `ExpectedReturnDate` trong `BorrowTicket` đúng bằng ngày đã nhập trên giao diện.

Kiểm tra SQL:

```sql
SELECT TOP 1 * FROM BorrowTickets ORDER BY Id DESC;
SELECT TOP 10 * FROM BorrowDetails ORDER BY Id DESC;
SELECT Id, Status FROM BookCopies WHERE Id IN (<copy_id>);
```

### BORROW-02 - Mượn bản sao đã mượn

Dùng lại ID đã chuyển sang `Borrowed`.

**Thành công khi:**

- HTTP `400`.
- Không tạo ticket mới.
- Không tạo detail mới.
- Trạng thái bản sao không đổi.

### BORROW-03 - Vượt giới hạn 5 sách

Mượn sao cho tổng số sách đang ở ticket `Open` lớn hơn 5.

**Thành công khi:**

- HTTP `400`.
- Procedure rollback.
- Không ghi ticket/detail dở dang.

### BORROW-04 - Có nợ phạt chưa trả

Trong database test, tạo FineRecord:

```sql
INSERT INTO FineRecords
  (BorrowTicketId, UserId, FineAmount, Reason, IsPaid)
VALUES
  (<ticket_id>, <user_id>, 5000, N'Test unpaid fine', 0);
```

Đăng nhập đúng user và thử mượn.

**Thành công khi:**

- HTTP `400`.
- Response nói user còn unpaid fine.
- Không tạo ticket/detail mới.
- Sau đó dọn dữ liệu:

```sql
UPDATE FineRecords
SET IsPaid = 1
WHERE Reason = N'Test unpaid fine';
```

### BORROW-05 - ID không tồn tại hoặc danh sách trùng

Thử lần lượt `999999` và `1,1`.

**Thành công khi:**

- HTTP `400`.
- Không có dữ liệu ghi một phần.
- Transaction được rollback.

### BORROW-06 - Kiểm thử đồng thời

Mở hai cửa sổ hoặc gửi hai request cùng lúc với cùng một BookCopy ID.

**Thành công khi:**

- Chỉ một request thành công.
- Request còn lại thất bại vì copy không còn Available.
- Database chỉ có một detail cho copy đó.

## 12. Kiểm thử trả sách và tiền phạt

### RETURN-01 - Trả đúng hạn

1. Lấy `BorrowTicketId` từ bảng phiếu mượn.
2. Nhập ID vào mục **Tiếp nhận trả sách**.
3. Chọn ngày hiện tại không muộn hơn hạn trả.
4. Chọn **Trả sách**.

**Thành công khi:**

- HTTP `200`.
- Ticket chuyển sang `Returned`.
- `ActualReturnDate` được ghi.
- Các BookCopy chuyển về `Available`.
- Không có FineRecord mới.

### RETURN-02 - Trả quá hạn

Chọn ngày trả sau `ExpectedReturnDate`.

**Thành công khi:**

- HTTP `200`.
- Ticket chuyển sang `Returned`.
- Có FineRecord `IsPaid = 0`.
- `FineAmount = số ngày quá hạn x 5.000`.
- Copy chuyển về `Available`.

### RETURN-03 - Trả hai lần

Gọi trả lại cùng ticket đã `Returned`.

**Thành công khi:**

- HTTP `404`.
- Không tạo thêm FineRecord.
- Không thay đổi dữ liệu cũ.

### RETURN-04 - Reader trả phiếu của người khác

Đăng nhập Reader A, tạo một ticket thuộc Reader B, sau đó gọi trả với
`BorrowTicketId` của Reader B.

**Thành công khi:**

- HTTP `404`.
- Ticket của Reader B vẫn `Open`.
- BookCopy vẫn giữ trạng thái `Borrowed`.
- Không tạo FineRecord.

### RETURN-05 - Trả sách một phần

API hiện tại nhận `BorrowTicketId` và trả **toàn bộ phiếu mượn** trong một lần;
không hỗ trợ trả từng `BookCopyId`. Đây là phạm vi được chọn cho yêu cầu trả
sách cấp phiếu.

**Thành công khi:** tạo ticket nhiều bản sao, gọi trả phiếu, tất cả bản sao
trong ticket chuyển sang `Available`, ticket chuyển `Returned`, và không còn
bản sao `Borrowed` thuộc ticket đó. Nếu cần trả từng cuốn, phải bổ sung trạng
thái trả riêng cho `BorrowDetail` và endpoint mới.

### FINE-01 - Admin/Librarian xác nhận thanh toán tiền phạt

1. Tạo `FineRecord` bằng cách trả sách quá hạn.
2. Đăng nhập bằng Admin hoặc Librarian.
3. Mở **Mượn & trả**, nhập `FineRecordId`.
4. Chọn **Xác nhận trả phạt**.

API:

```text
POST /api/borrow/fines/{fineId}/pay
```

**Thành công khi:**

- Admin/Librarian nhận HTTP `200`.
- `FineRecord.IsPaid` chuyển từ `0` sang `1`.
- Gọi lại endpoint không tạo bản ghi mới.
- Reader nhận HTTP `403`.
- ID không tồn tại nhận HTTP `404`.
- Sau khi thanh toán, user không còn khoản phạt chưa trả và có thể mượn lại.

Kiểm tra:

```sql
SELECT Id, UserId, FineAmount, IsPaid
FROM FineRecords
WHERE Id = <fine_id>;
```

## 13. Kiểm thử quản lý người dùng

Đăng nhập Admin, mở **Độc giả**.

| Thao tác | Thành công khi |
|---|---|
| Tạo Reader | HTTP 200, role Reader |
| Tạo Librarian | HTTP 200, role Librarian |
| Tạo Admin | HTTP 200, role Admin |
| Tạo username trùng | HTTP 400 |
| Role không hợp lệ | HTTP 400 |
| Xem danh sách user | HTTP 200 |
| Vô hiệu hóa user | HTTP 204, status Inactive |
| Login user Inactive | HTTP 401 |
| Reader gọi `/api/users` | HTTP 403 |

## 14. Kiểm thử Global Exception và Serilog

### Các bước

1. Gọi resource không tồn tại.
2. Gửi dữ liệu sai validation.
3. Gửi request mượn vi phạm nghiệp vụ.
4. Mở menu **Request log**.
5. Kiểm tra file log:

```powershell
Get-Content .\src\LibraryAPI\logs\library-api-*.log -Tail 50
```

### Thành công khi

- Lỗi validation trả HTTP `400`.
- Resource không tồn tại trả HTTP `404`.
- Lỗi authentication trả HTTP `401`.
- Lỗi authorization trả HTTP `403`.
- Lỗi nghiệp vụ trả JSON có `error`, `detail`, `traceId`.
- Lỗi server trả HTTP `500` nhưng không lộ stack trace.
- Serilog ghi được request, status và exception.

## 15. Kiểm thử IIS

### Các bước

1. Publish:

```powershell
dotnet publish .\src\LibraryAPI\LibraryAPI.csproj -c Release -o .\publish
```

2. Cấu hình IIS Application Pool là **No Managed Code**.
3. Trỏ website tới thư mục `publish`.
4. Cấp `IIS_IUSRS` quyền Read/Execute.
5. Cấp quyền Modify cho thư mục `publish\logs`.
6. Mở địa chỉ IIS.
7. Chạy lại các nhóm AUTH, RBAC, BOOK, COPY, BORROW, RETURN và USER.

### Thành công khi

- Mở `/` nhận HTTP `200`.
- Dashboard hiển thị đúng.
- `/api/books` nhận HTTP `200`.
- Login nhận JWT.
- API protected kiểm tra đúng role.
- Mượn/trả ghi đúng database.
- Log được tạo trong thư mục IIS có quyền ghi.
- Restart Application Pool không làm ứng dụng lỗi.

## 16. Tiêu chí kết luận cuối cùng

Đánh giá hệ thống **PASS** khi tất cả điều kiện sau đúng:

1. Build không có error.
2. Dashboard và `/api/books` trả HTTP `200`.
3. CRUD sách, bản sao và người dùng hoạt động đúng.
4. JWT được phát hành khi đăng nhập hợp lệ và bị từ chối khi sai.
5. RBAC chặn đúng Reader/Librarian/Admin.
6. Dữ liệu sai bị FluentValidation từ chối.
7. Stored procedure kiểm tra nợ phạt, giới hạn 5 sách và trạng thái copy.
8. Transaction không tạo dữ liệu dở dang khi có lỗi.
9. Trả sách cập nhật ticket/copy và tạo phạt đúng số ngày.
10. Global Exception trả JSON chuẩn, không lộ stack trace.
11. Serilog ghi log hệ thống.
12. Ứng dụng chạy ổn định sau publish lên Local IIS.

Nếu chỉ một điều kiện trên không đạt, kết luận là **FAIL** và cần ghi lại
test case, dữ liệu, response, log và ảnh màn hình để sửa lỗi.
