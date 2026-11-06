CREATE OR ALTER PROCEDURE dbo.sp_BorrowBook
    @UserId INT,
    @BookCopyIds NVARCHAR(MAX),
    @ExpectedReturnDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Id = @UserId AND Status = N'Active')
            THROW 51000, 'User does not exist or is inactive.', 1;
        IF EXISTS (SELECT 1 FROM dbo.FineRecords WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @UserId AND IsPaid = 0)
            THROW 51001, 'User has unpaid fines.', 1;

        DECLARE @Requested TABLE (BookCopyId INT PRIMARY KEY);
        INSERT INTO @Requested (BookCopyId)
        SELECT DISTINCT TRY_CONVERT(INT, LTRIM(RTRIM(value)))
        FROM STRING_SPLIT(@BookCopyIds, N',')
        WHERE TRY_CONVERT(INT, LTRIM(RTRIM(value))) IS NOT NULL;
        DECLARE @RequestedCount INT = (SELECT COUNT(*) FROM @Requested);
        IF @RequestedCount = 0 OR @RequestedCount <> (SELECT COUNT(*) FROM STRING_SPLIT(@BookCopyIds, N','))
            THROW 51002, 'Book copy list is invalid.', 1;
        IF @ExpectedReturnDate IS NULL OR @ExpectedReturnDate <= SYSUTCDATETIME()
            THROW 51005, 'Expected return date must be in the future.', 1;
        IF @RequestedCount + (
            SELECT COUNT(*) FROM dbo.BorrowDetail d
            INNER JOIN dbo.BorrowTicket t ON t.Id = d.BorrowTicketId
            WHERE t.UserId = @UserId AND t.Status = N'Open'
        ) > 5
            THROW 51003, 'Borrow limit is five books.', 1;
        IF EXISTS (
            SELECT 1 FROM @Requested r
            LEFT JOIN dbo.BookCopies c WITH (UPDLOCK, HOLDLOCK) ON c.Id = r.BookCopyId
            WHERE c.Id IS NULL OR c.Status <> N'Available'
        )
            THROW 51004, 'One or more book copies are unavailable.', 1;

        DECLARE @BorrowTicketId INT;
        INSERT INTO dbo.BorrowTickets (UserId, BorrowDate, ExpectedReturnDate, Status)
        VALUES (@UserId, SYSUTCDATETIME(), @ExpectedReturnDate, N'Open');
        SET @BorrowTicketId = SCOPE_IDENTITY();
        INSERT INTO dbo.BorrowDetails (BorrowTicketId, BookCopyId)
        SELECT @BorrowTicketId, BookCopyId FROM @Requested;
        UPDATE c SET Status = N'Borrowed'
        FROM dbo.BookCopies c INNER JOIN @Requested r ON r.BookCopyId = c.Id;
        COMMIT;
        SELECT @BorrowTicketId AS BorrowTicketId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
