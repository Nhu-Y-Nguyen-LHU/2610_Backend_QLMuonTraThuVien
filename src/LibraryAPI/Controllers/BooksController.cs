using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController, Route("api/books")]
public sealed class BooksController(IBookService service) : ControllerBase
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken) => Ok(await service.SearchAsync(q, cancellationToken));
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) => (await service.GetAsync(id, cancellationToken)) is { } book ? Ok(book) : NotFound();
    [Authorize(Roles = "Admin,Librarian"), HttpPost]
    public async Task<IActionResult> Create(CreateBookDto request, CancellationToken cancellationToken) => CreatedAtAction(nameof(Get), new { id = (await service.CreateAsync(request, cancellationToken)).Id }, request);
    [Authorize(Roles = "Admin,Librarian"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateBookDto request, CancellationToken cancellationToken) => await service.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound();
    [Authorize(Roles = "Admin,Librarian"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    [Authorize(Roles = "Admin,Librarian"), HttpPost("copies")]
    public async Task<IActionResult> AddCopy(CreateCopyDto request, CancellationToken cancellationToken) => Ok(await service.AddCopyAsync(request, cancellationToken));
}
