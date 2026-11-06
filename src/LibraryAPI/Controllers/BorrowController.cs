using System.Security.Claims;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController, Route("api/borrow")]
public sealed class BorrowController(IBorrowService service) : ControllerBase
{
    [Authorize(Roles = "Admin,Librarian,Reader"), HttpGet("tickets")]
    public async Task<IActionResult> Tickets(CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
        int? userId = isStaff ? null : int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await service.GetTicketsAsync(userId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Librarian,Reader"), HttpGet("fines")]
    public async Task<IActionResult> Fines(CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
        int? userId = isStaff ? null : int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await service.GetFinesAsync(userId, cancellationToken));
    }

    [Authorize(Roles = "Admin,Librarian"), HttpPost("fines/{fineId:int}/pay")]
    public async Task<IActionResult> PayFine(int fineId, CancellationToken cancellationToken) =>
        await service.PayFineAsync(fineId, cancellationToken)
            ? Ok(new { message = "Fine marked as paid.", fineId })
            : NotFound();

    [Authorize(Roles = "Admin,Librarian,Reader"), HttpPost]
    public async Task<IActionResult> Borrow(BorrowRequestDto request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? throw new UnauthorizedAccessException());
        return Ok(new { borrowTicketId = await service.BorrowAsync(userId, request, cancellationToken) });
    }
    [Authorize(Roles = "Admin,Librarian,Reader"), HttpPost("{ticketId:int}/return")]
    public async Task<IActionResult> Return(int ticketId, ReturnBookDto request, CancellationToken cancellationToken)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
        return await service.ReturnAsync(ticketId, request.ReturnDate, requesterId, isStaff, cancellationToken)
            ? Ok(new { message = "Book returned." })
            : NotFound();
    }
}
