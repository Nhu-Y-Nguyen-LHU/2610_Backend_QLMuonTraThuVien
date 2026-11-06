using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

[ApiController, Route("api/users"), Authorize(Roles = "Admin")]
public sealed class UsersController(IUserService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await service.GetAllAsync(cancellationToken));
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) => (await service.GetAsync(id, cancellationToken)) is { } user ? Ok(user) : NotFound();
    [HttpPost] public async Task<IActionResult> Create(CreateUserDto request, CancellationToken cancellationToken) => Ok(await service.CreateAsync(request, cancellationToken));
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, UpdateUserDto request, CancellationToken cancellationToken) => await service.UpdateAsync(id, request, cancellationToken) ? NoContent() : NotFound();
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
