using System.Data;
using Microsoft.Data.SqlClient;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public sealed class BorrowService(AppDbContext db) : IBorrowService
{
    public async Task<IReadOnlyList<BorrowTicket>> GetTicketsAsync(int? userId, CancellationToken cancellationToken) =>
        await db.BorrowTickets.Include(x => x.Details).ThenInclude(x => x.BookCopy).Include(x => x.User)
            .Where(x => userId == null || x.UserId == userId).OrderByDescending(x => x.Id).AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FineRecord>> GetFinesAsync(int? userId, CancellationToken cancellationToken) =>
        await db.FineRecords.Include(x => x.User).Include(x => x.BorrowTicket)
            .Where(x => userId == null || x.UserId == userId).OrderByDescending(x => x.Id).AsNoTracking().ToListAsync(cancellationToken);

    public async Task<bool> PayFineAsync(int fineId, CancellationToken cancellationToken)
    {
        var fine = await db.FineRecords.SingleOrDefaultAsync(x => x.Id == fineId, cancellationToken);
        if (fine is null) return false;
        if (!fine.IsPaid)
        {
            fine.IsPaid = true;
            await db.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<int> BorrowAsync(int userId, BorrowRequestDto request, CancellationToken cancellationToken)
    {
        if (request.BookCopyIds.Distinct().Count() != request.BookCopyIds.Count) throw new InvalidOperationException("Duplicate book copies are not allowed.");
        var ids = string.Join(",", request.BookCopyIds);
        var userParameter = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
        var copiesParameter = new SqlParameter("@BookCopyIds", SqlDbType.NVarChar, -1) { Value = ids };
        var expectedReturnParameter = new SqlParameter("@ExpectedReturnDate", SqlDbType.DateTime2) { Value = request.ExpectedReturnDate.ToUniversalTime() };
        var result = await db.BorrowProcedureResults
            .FromSqlRaw("EXEC dbo.sp_BorrowBook @UserId, @BookCopyIds, @ExpectedReturnDate", userParameter, copiesParameter, expectedReturnParameter)
            .AsNoTracking().SingleAsync(cancellationToken);
        return result.BorrowTicketId;
    }

    public async Task<bool> ReturnAsync(int ticketId, DateTime returnDate, int requesterId, bool requesterIsStaff, CancellationToken cancellationToken)
    {
        var ticket = await db.BorrowTickets.Include(x => x.Details).ThenInclude(x => x.BookCopy).SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null || ticket.Status == RecordStatuses.Returned || (!requesterIsStaff && ticket.UserId != requesterId)) return false;
        var overdueDays = Math.Max(0, (returnDate.Date - ticket.ExpectedReturnDate.Date).Days);
        if (overdueDays > 0) db.FineRecords.Add(new FineRecord { BorrowTicketId = ticket.Id, UserId = ticket.UserId, FineAmount = overdueDays * 5000m, Reason = $"Overdue by {overdueDays} day(s).", IsPaid = false });
        foreach (var detail in ticket.Details) detail.BookCopy.Status = RecordStatuses.Available;
        ticket.ActualReturnDate = returnDate; ticket.Status = RecordStatuses.Returned;
        await db.SaveChangesAsync(cancellationToken); return true;
    }
}
