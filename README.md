# Library Management API

ASP.NET Core 8 layered API for library borrowing and returns. Run from `src\LibraryAPI` with `dotnet run`; Swagger is enabled in Development and the API test dashboard is served at `/`. The dashboard includes authentication/RBAC, book/copy CRUD, user management, borrow/return/fine workflows, automated negative tests, and an in-browser request log.

After each feature change, run `powershell -ExecutionPolicy Bypass -File .\scripts\build-test-open.ps1`. The script builds the API, starts it on port 5187 when needed, runs dashboard/books smoke tests, and opens the browser automatically.

Detailed manual test cases, steps, expected results, and PASS/FAIL criteria are
available in [`TEST-CASES.md`](TEST-CASES.md).

Create/update the schema with `dotnet ef database update --project src\LibraryAPI`. The application applies pending migrations at startup and then executes [`sp_BorrowBook`](database/sp_BorrowBook.sql) automatically, including for an existing database where the procedure is missing.
