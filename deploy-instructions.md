# Hướng dẫn triển khai và kiểm thử

Tài liệu này dùng để chứng minh các tiêu chí của đề tài: kiến trúc phân tầng, EF
Core Code-First, Stored Procedure có transaction, JWT/RBAC, validation,
exception/logging và chạy trên Local IIS.

## 1. Chuẩn bị môi trường

Máy Windows cần có:

- .NET 8 SDK để build/migration.
- SQL Server Express/Developer và SQL Server Management Studio (SSMS).
- IIS, ASP.NET Core/.NET 8 Hosting Bundle.
- PowerShell 5+.

Mở PowerShell tại thư mục repository:

```powershell
cd D:\Code\2610_Backend_QLMuonTraThuVien
dotnet --version
dotnet restore .\src\LibraryAPI\LibraryAPI.csproj
```

## 2. Tạo database bằng EF Core Code-First

Nếu chưa cài EF CLI:

```powershell
dotnet tool install --global dotnet-ef
```

Tạo migration và database:

```powershell
dotnet ef database update --project .\src\LibraryAPI\LibraryAPI.csproj --startup-project .\src\LibraryAPI\LibraryAPI.csproj
```

Ứng dụng sẽ áp dụng migration rồi tự động thực thi
[`database/sp_BorrowBook.sql`](database/sp_BorrowBook.sql) khi khởi động. Vì
vậy database hiện có nhưng thiếu procedure cũng được bổ sung tự động. File SQL
vẫn được giữ lại để kiểm tra hoặc cài đặt thủ công trên database ngoài ứng dụng.

Connection string mặc định nằm trong
[`src/LibraryAPI/appsettings.json`](src/LibraryAPI/appsettings.json). Với SQL
Server dùng tài khoản Windows, giữ `Trusted_Connection=True`. Nếu dùng SQL
Authentication, thay bằng `User Id=...;Password=...` và không đưa mật khẩu vào
Git.

## 3. Kiểm tra Stored Procedure

Khởi động API hoặc chạy `dotnet ef database update`, sau đó khởi động API để
initializer thực thi procedure. Xác minh:

```sql
USE LibraryManagementDb;
SELECT name
FROM sys.procedures
WHERE name = N'sp_BorrowBook';
```

Procedure thực hiện trong một transaction:

1. Khóa và kiểm tra độc giả đang hoạt động.
2. Từ chối nếu có `FineRecord.IsPaid = 0`.
3. Từ chối nếu tổng sách đang mượn và sách mới vượt quá 5.
4. Khóa và kiểm tra mọi bản sao có trạng thái `Available`.
5. Ghi `BorrowTicket`, `BorrowDetail` và cập nhật `BookCopy.Status`.
6. `COMMIT` khi thành công; `ROLLBACK` và `THROW` khi lỗi.

## 4. Seed dữ liệu kiểm thử

Đăng ký một Reader qua API trước. Vì endpoint tạo user yêu cầu token Admin, cách
khởi tạo tài khoản quản trị đầu tiên trong môi trường demo là:

1. Đăng ký user `admin` trên dashboard (tài khoản ban đầu là `Reader`).
2. Trong SSMS, chạy lệnh sau:

```sql
USE LibraryManagementDb;
UPDATE Users SET Role = N'Admin' WHERE Username = N'admin';
```

3. Đăng xuất rồi đăng nhập lại `admin` để JWT mới chứa role `Admin`.
4. Sau khi đăng nhập Admin, tạo đầu sách
qua API, sau đó tạo các bản sao bằng endpoint `POST /api/books/copies`.

Không dùng cách cập nhật role trực tiếp này trong production. Đây chỉ là thao
tác bootstrap dữ liệu demo/test.

## 5. Chạy và kiểm thử local trước IIS

Build:

```powershell
dotnet build .\src\LibraryAPI\LibraryAPI.csproj
```

Chạy:

```powershell
dotnet run --project .\src\LibraryAPI\LibraryAPI.csproj --urls http://localhost:5187
```

Mở `http://localhost:5187/` để dùng dashboard hoặc
`http://localhost:5187/swagger` khi chạy Development.

### Lệnh một bước sau mỗi lần phát triển tính năng

Từ thư mục repository, chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1
```

Script sẽ tự động:

1. Build project.
2. Dùng API đang chạy ở cổng 5187 nếu đã có; nếu chưa thì khởi động API từ DLL.
3. Kiểm thử `GET /` và `GET /api/books`.
4. Mở trình duyệt mặc định tại `http://localhost:5187`.

Log stdout/stderr của tiến trình phát triển được ghi tại
`src\LibraryAPI\logs\development-server.out.log` và
`src\LibraryAPI\logs\development-server.err.log`. Nếu cần dừng tiến trình được script
khởi động, chạy lại với `-StopServer` sau khi API đã sẵn sàng hoặc dừng PID được
in trên màn hình bằng Task Manager.

### 5.1 Kiểm thử đăng ký/đăng nhập JWT

```powershell
$base = "http://localhost:5187"
$register = @{
  username = "reader01"
  password = "Reader@12345"
  fullName = "Nguyen Van Reader"
} | ConvertTo-Json

Invoke-RestMethod "$base/api/auth/register" -Method Post -ContentType "application/json" -Body $register

$login = @{
  username = "reader01"
  password = "Reader@12345"
} | ConvertTo-Json
$auth = Invoke-RestMethod "$base/api/auth/login" -Method Post -ContentType "application/json" -Body $login
$token = $auth.token
$headers = @{ Authorization = "Bearer $token" }
```

Kết quả cần đạt: response có `userId`, `role`, `token`; token được dùng ở các
request protected.

### 5.2 Kiểm thử tra cứu sách

```powershell
Invoke-RestMethod "$base/api/books?q=dotnet" -Method Get
```

Endpoint này cho phép anonymous và trả về đầu sách cùng danh sách `copies`,
bao gồm `Id`, `Barcode`, `Status`.

### 5.3 Kiểm thử RBAC

```powershell
# Không có token: phải nhận 401
try { Invoke-RestMethod "$base/api/users" -Method Get } catch { $_.Exception.Response.StatusCode }

# Reader gọi quản trị user: phải nhận 403
try { Invoke-RestMethod "$base/api/users" -Method Get -Headers $headers } catch { $_.Exception.Response.StatusCode }
```

Tạo token Admin riêng và xác minh `GET/POST/PUT/DELETE /api/users` hoạt động.
Các endpoint quản lý đầu sách chỉ chấp nhận `Admin,Librarian`; endpoint mượn/trả
chấp nhận cả ba role.

### 5.4 Kiểm thử lập phiếu mượn và transaction

```powershell
$borrow = @{
  bookCopyIds = @(1, 2)
  expectedReturnDate = (Get-Date).ToUniversalTime().AddDays(14).ToString("o")
} | ConvertTo-Json
$ticket = Invoke-RestMethod "$base/api/borrow" -Method Post `
  -Headers $headers -ContentType "application/json" -Body $borrow
$ticket
```

Xác minh trong SQL Server:

```sql
SELECT * FROM BorrowTickets ORDER BY Id DESC;
SELECT * FROM BorrowDetails ORDER BY Id DESC;
SELECT Id, Barcode, Status FROM BookCopies WHERE Id IN (1, 2);
```

Kết quả cần đạt: có một ticket, hai detail và trạng thái bản sao chuyển sang
`Borrowed`.

Chạy lại với cùng bản sao: phải thất bại và không tạo thêm ticket. Thử các
trường hợp sau:

- Reader có `FineRecords.IsPaid = 0`: bị từ chối.
- Mượn tổng cộng hơn 5 bản sao: bị từ chối.
- Danh sách có bản sao không tồn tại hoặc không phải `Available`: bị từ chối.
- Danh sách có ID trùng: bị từ chối.

Sau mỗi ca thất bại, kiểm tra `BorrowTickets`, `BorrowDetails` và
`BookCopies`; không được có dữ liệu ghi dở dang.

### 5.5 Kiểm thử trả sách và tiền phạt

```powershell
$return = @{ returnDate = (Get-Date).ToUniversalTime().ToString("o") } | ConvertTo-Json
Invoke-RestMethod "$base/api/borrow/$($ticket.borrowTicketId)/return" `
  -Method Post -Headers $headers -ContentType "application/json" -Body $return
```

Để kiểm thử quá hạn, gửi `returnDate` sau `ExpectedReturnDate`. Khi đó phải có
`FineRecord` với số tiền `5000` cho mỗi ngày quá hạn, bản sao trở lại
`Available`, và ticket chuyển `Returned`.

### 5.6 Kiểm thử validation, exception và logging

```powershell
$invalid = '{"bookCopyIds":[],"expectedReturnDate":"2000-01-01T00:00:00Z"}'
try { Invoke-RestMethod "$base/api/borrow" -Method Post -Headers $headers `
  -ContentType "application/json" -Body $invalid } catch { $_.ErrorDetails.Message }
```

Kết quả phải là HTTP 400 với lỗi validation. Lỗi nghiệp vụ cũng trả JSON có
`error`, `detail`, `traceId`; lỗi ngoài dự kiến trả HTTP 500 và không lộ
stack trace. Serilog ghi log rolling tại `src\LibraryAPI\logs\`.

## 6. Publish lên Local IIS

Đặt secret production qua biến môi trường hoặc secret manager; không dùng JWT
key mẫu trong `appsettings.json`.

```powershell
dotnet publish .\src\LibraryAPI\LibraryAPI.csproj -c Release -o .\publish
```

Trong IIS:

1. Cài .NET 8 Hosting Bundle và khởi động lại IIS.
2. Tạo Application Pool `LibraryAPI`, chọn **No Managed Code** và pipeline
   `Integrated`.
3. Tạo website/application trỏ tới thư mục `publish`.
4. Cấp `IIS_IUSRS` quyền Read & Execute trên `publish`.
5. Cấp Modify trên `publish\logs` để Serilog ghi log.
6. Kiểm tra `web.config` đã được publish cùng `LibraryAPI.dll`.
7. Mở `http://localhost:<port>/` và gọi `GET /api/books`.

Lệnh kiểm tra sau publish:

```powershell
Invoke-WebRequest "http://localhost:<port>/" -UseBasicParsing
Invoke-RestMethod "http://localhost:<port>/api/books"
```

Nếu nhận HTTP 500 khi IIS khởi động, kiểm tra Event Viewer, quyền thư mục và
connection string. Chỉ bật `stdoutLogEnabled="true"` trong `web.config` để chẩn
đoán tạm thời, sau đó tắt lại.

## 7. Checklist nghiệm thu

| Tiêu chí | Minh chứng |
|---|---|
| Phân tầng và DI | `Controllers -> Services -> AppDbContext`, constructor injection |
| EF Core Code-First | 6 entity nghiệp vụ, migration và CRUD |
| Stored Procedure/Transaction | `database/sp_BorrowBook.sql`, `TRY...CATCH`, `BEGIN TRAN`, `COMMIT`, `ROLLBACK` |
| JWT/RBAC | `AuthService`, Bearer authentication, Admin/Librarian/Reader |
| Validation | FluentValidation trong `DTOs/Dtos.cs` |
| Global Exception/Serilog | `GlobalExceptionMiddleware.cs`, rolling file log |
| UI kiểm thử | `src/LibraryAPI/wwwroot/index.html` |
| IIS | `web.config`, publish Release, Application Pool No Managed Code |

## 8. Ma trận kiểm thử đầy đủ

Thực hiện các ca dưới đây trên dashboard tại `http://localhost:5187/`. Các
request và response cũng được lưu trong mục **Request log** để chụp màn hình
làm minh chứng. Trước khi bắt đầu, xóa dữ liệu test cũ hoặc dùng username,
ISBN và barcode mới cho mỗi lần chạy.

### 8.1. Authentication và validation tài khoản

| Mã | Thao tác | Kết quả mong đợi |
|---|---|---|
| AUTH-01 | Đăng ký Reader với username, password từ 8 ký tự, full name hợp lệ | HTTP 200, nhận JWT và role `Reader` |
| AUTH-02 | Đăng nhập đúng username/password | HTTP 200, token dùng được |
| AUTH-03 | Đăng nhập sai password | HTTP 401, không phát hành token |
| AUTH-04 | Đăng nhập username không tồn tại | HTTP 401 |
| AUTH-05 | Đăng ký username đã tồn tại | HTTP 400, không tạo bản ghi thứ hai |
| AUTH-06 | Đăng ký username rỗng hoặc dài hơn 100 ký tự | HTTP 400 validation |
| AUTH-07 | Đăng ký password ngắn hơn 8 ký tự | HTTP 400 validation |
| AUTH-08 | Đăng ký full name rỗng hoặc dài hơn 150 ký tự | HTTP 400 validation |
| AUTH-09 | Xóa JWT trong trình duyệt rồi gọi API protected | HTTP 401 |
| AUTH-10 | Dùng JWT hết hạn hoặc token bị sửa | HTTP 401 |

### 8.2. RBAC

Tạo hoặc chuẩn bị ba tài khoản: `Admin`, `Librarian`, `Reader`. Bảng sau là
quyền tối thiểu cần xác minh:

| Endpoint | Admin | Librarian | Reader | Anonymous |
|---|---:|---:|---:|---:|
| `GET /api/books` | 200 | 200 | 200 | 200 |
| `POST/PUT/DELETE /api/books` | 2xx | 2xx | 403 | 401 |
| `POST /api/books/copies` | 2xx | 2xx | 403 | 401 |
| `POST /api/borrow` | 2xx/400 nghiệp vụ | 2xx/400 nghiệp vụ | 2xx/400 nghiệp vụ | 401 |
| `POST /api/borrow/{id}/return` | 2xx | 2xx | 2xx | 401 |
| `GET /api/borrow/tickets` | Tất cả ticket | Tất cả ticket | Chỉ ticket của mình | 401 |
| `GET /api/borrow/fines` | Tất cả phạt | Tất cả phạt | Chỉ phạt của mình | 401 |
| `/api/users` | 2xx | 403 | 403 | 401 |

Trong dashboard, dùng trang **Kiểm thử yêu cầu** để chạy nhanh ca không token,
Reader gọi API Admin, dữ liệu mượn sai, 404 và API public.

### 8.3. CRUD đầu sách và bản sao

| Mã | Thao tác | Kết quả mong đợi |
|---|---|---|
| BOOK-01 | Tạo sách với tất cả trường hợp lệ | HTTP 201, có `Id` |
| BOOK-02 | Tạo ISBN trùng | HTTP 400, không tạo thêm |
| BOOK-03 | Title/Author/ISBN rỗng | HTTP 400 validation |
| BOOK-04 | `TotalCopies` âm | HTTP 400 validation |
| BOOK-05 | Cập nhật sách tồn tại | HTTP 204, dữ liệu thay đổi |
| BOOK-06 | Cập nhật ID không tồn tại | HTTP 404 |
| BOOK-07 | Xóa sách tồn tại chưa có ràng buộc | HTTP 204 |
| BOOK-08 | Tra cứu theo title, author và ISBN | HTTP 200, kết quả đúng |
| BOOK-09 | Tra cứu không có kết quả | HTTP 200, mảng rỗng |
| COPY-01 | Thêm bản sao với `BookTitleId` hợp lệ | HTTP 200, status `Available` |
| COPY-02 | Thêm barcode trùng | HTTP 400 |
| COPY-03 | Thêm bản sao với `BookTitleId` không tồn tại | HTTP 404/400 |
| COPY-04 | Tra cứu sách và xem barcode/status từng bản sao | HTTP 200, dữ liệu hiển thị trên bảng |

### 8.4. Stored Procedure và transaction mượn sách

Tạo tối thiểu 6 bản sao `Available` thuộc một hoặc nhiều đầu sách. Dùng trang
**Mượn & trả**, nhập ID bản sao cách nhau bằng dấu phẩy.

Để tạo dữ liệu cho ca nợ phạt, lấy `UserId` và `BorrowTicketId` từ database rồi
thực hiện trong môi trường test:

```sql
INSERT INTO FineRecords (BorrowTicketId, UserId, FineAmount, Reason, IsPaid)
VALUES (<ticket_id>, <user_id>, 5000, N'Test unpaid fine', 0);
```

Sau ca `BORROW-08`, dọn dữ liệu hoặc đánh dấu đã thanh toán:

```sql
UPDATE FineRecords SET IsPaid = 1 WHERE Reason = N'Test unpaid fine';
```

| Mã | Dữ liệu/thao tác | Kết quả mong đợi |
|---|---|---|
| BORROW-01 | Mượn 1 bản sao Available | Tạo 1 `BorrowTicket`, 1 `BorrowDetail`, copy thành `Borrowed` |
| BORROW-02 | Mượn nhiều bản sao Available | Tạo đúng số detail trong cùng nghiệp vụ |
| BORROW-03 | Mượn lại copy đã `Borrowed` | HTTP 400; không thêm ticket/detail |
| BORROW-04 | Mượn copy `Lost` | HTTP 400; không thêm ticket/detail |
| BORROW-05 | Truyền ID không tồn tại | HTTP 400; transaction rollback |
| BORROW-06 | Truyền danh sách rỗng | HTTP 400 validation |
| BORROW-07 | Truyền ID trùng, ví dụ `1,1` | HTTP 400; không ghi dữ liệu |
| BORROW-08 | Mượn khi đang có `FineRecord.IsPaid = 0` | HTTP 400; không ghi dữ liệu |
| BORROW-09 | Có 4 sách đang mượn, mượn thêm 2 | HTTP 400 vì vượt giới hạn 5 |
| BORROW-10 | Có 4 sách đang mượn, mượn thêm 1 | Thành công |
| BORROW-11 | Mượn 6 sách trong một request | HTTP 400 validation/SP |
| BORROW-12 | Hai request đồng thời cùng một copy | Chỉ một request thành công; request còn lại thất bại |
| BORROW-13 | Sau mọi ca thất bại | Không có ticket/detail dở dang; copy vẫn giữ trạng thái cũ |

Sau mỗi ca `BORROW-03` đến `BORROW-13`, chạy SQL:

```sql
SELECT TOP 10 * FROM BorrowTickets ORDER BY Id DESC;
SELECT TOP 20 * FROM BorrowDetails ORDER BY Id DESC;
SELECT Id, Barcode, Status FROM BookCopies ORDER BY Id;
```

Đây là minh chứng cho `BEGIN TRAN`, `COMMIT` và `ROLLBACK`.

### 8.5. Trả sách và tiền phạt

Để kiểm thử quá hạn, tạo ticket thành công trước, sau đó gửi ngày trả lớn hơn
`ExpectedReturnDate`. Không sửa trực tiếp ticket đang dùng cho ca đúng hạn nếu
cần giữ lại minh chứng; hãy tạo một ticket test riêng.

| Mã | Thao tác | Kết quả mong đợi |
|---|---|---|
| RETURN-01 | Trả ticket đang `Open` đúng hạn | HTTP 200, ticket `Returned`, copy `Available`, không tạo phạt |
| RETURN-02 | Trả ticket quá hạn 1 ngày | HTTP 200, tạo FineRecord `5000` |
| RETURN-03 | Trả ticket quá hạn nhiều ngày | FineRecord = số ngày x 5.000 |
| RETURN-04 | Trả ticket đã `Returned` lần hai | HTTP 404, không tạo phạt thứ hai |
| RETURN-05 | Trả ticket không tồn tại | HTTP 404 |
| RETURN-06 | Reader xem danh sách ticket/fine | Chỉ thấy dữ liệu của chính mình |
| RETURN-07 | Librarian/Admin xem ticket/fine | Thấy toàn bộ dữ liệu |
| RETURN-08 | Kiểm tra bản sao sau trả | Mọi copy trong ticket trở về `Available` |

SQL xác minh:

```sql
SELECT * FROM BorrowTickets WHERE Id = <ticket_id>;
SELECT * FROM BorrowDetails WHERE BorrowTicketId = <ticket_id>;
SELECT * FROM FineRecords WHERE BorrowTicketId = <ticket_id>;
```

### 8.6. Quản lý người dùng

| Mã | Thao tác | Kết quả mong đợi |
|---|---|---|
| USER-01 | Admin tạo Reader/Librarian/Admin | HTTP 200, role đúng |
| USER-02 | Username trùng | HTTP 400 |
| USER-03 | Role ngoài `Admin/Librarian/Reader` | HTTP 400 |
| USER-04 | Admin cập nhật full name, role, status | HTTP 204 |
| USER-05 | Admin xóa user | HTTP 204, user chuyển `Inactive` |
| USER-06 | Reader/Librarian gọi `/api/users` | HTTP 403 |
| USER-07 | Truy cập user ID không tồn tại | HTTP 404 |
| USER-08 | Đăng nhập user `Inactive` | HTTP 401 |

### 8.7. Global exception, validation và logging

| Mã | Thao tác | Kết quả mong đợi |
|---|---|---|
| QUALITY-01 | Gửi JSON sai kiểu dữ liệu | HTTP 400, JSON error rõ ràng |
| QUALITY-02 | Gửi ngày trả không hợp lệ/quá khứ | HTTP 400 validation |
| QUALITY-03 | Gọi resource không tồn tại | HTTP 404 |
| QUALITY-04 | Lỗi nghiệp vụ từ service/SP | HTTP 400, có `error`, `detail`, `traceId` |
| QUALITY-05 | Lỗi không dự kiến | HTTP 500, không lộ stack trace |
| QUALITY-06 | Kiểm tra sau request | Serilog có log request và lỗi |

Kiểm tra file log:

```powershell
Get-ChildItem .\src\LibraryAPI\logs
Get-Content .\src\LibraryAPI\logs\library-api-*.log -Tail 50
```

### 8.8. Kiểm thử Local IIS sau publish

```powershell
dotnet publish .\src\LibraryAPI\LibraryAPI.csproj -c Release -o .\publish
```

Sau khi tạo IIS site, chạy lại toàn bộ nhóm `AUTH`, `RBAC`, `BOOK`, `COPY`,
`BORROW`, `RETURN`, `USER` với base URL của IIS thay cho `localhost:5187`.

| Mã | Kiểm tra IIS | Kết quả mong đợi |
|---|---|---|
| IIS-01 | Mở `/` | Dashboard HTTP 200 |
| IIS-02 | `GET /api/books` | HTTP 200 |
| IIS-03 | Login và gọi endpoint protected | JWT hoạt động |
| IIS-04 | Ghi log sau request | IIS user có quyền ghi thư mục logs |
| IIS-05 | Restart Application Pool | Ứng dụng khởi động lại bình thường |
| IIS-06 | Sai connection string | Có lỗi rõ trong Event Viewer/log, không trang trắng |

## 9. Tiêu chí đạt

Một lần kiểm thử được xem là đạt khi:

- Tất cả ca `AUTH-01..10`, `RBAC`, `BOOK`, `COPY`, `BORROW`, `RETURN`, `USER`
  có kết quả đúng bảng.
- Mọi ca thất bại của stored procedure không tạo dữ liệu dở dang.
- JWT không bị trả về khi login sai hoặc user inactive.
- Reader không truy cập được API Admin.
- Log có đủ request thành công và lỗi.
- Kết quả local và IIS giống nhau.
