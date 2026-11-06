using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public sealed class UserService(AppDbContext db) : IUserService
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) => await db.Users.AsNoTracking().ToListAsync(cancellationToken);
    public Task<User?> GetAsync(int id, CancellationToken cancellationToken) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<User> CreateAsync(CreateUserDto request, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(x => x.Username == request.Username, cancellationToken)) throw new InvalidOperationException("Username already exists.");
        if (request.Role is not (Roles.Admin or Roles.Librarian or Roles.Reader)) throw new InvalidOperationException("Invalid role.");
        var user = new User { Username = request.Username, FullName = request.FullName, Role = request.Role, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) };
        db.Users.Add(user); await db.SaveChangesAsync(cancellationToken); return user;
    }
    public async Task<bool> UpdateAsync(int id, UpdateUserDto request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken); if (user is null) return false;
        if (request.Role is not (Roles.Admin or Roles.Librarian or Roles.Reader) || request.Status is not (RecordStatuses.Active or RecordStatuses.Inactive)) throw new InvalidOperationException("Invalid role or status.");
        user.FullName = request.FullName; user.Role = request.Role; user.Status = request.Status; await db.SaveChangesAsync(cancellationToken); return true;
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken); if (user is null) return false;
        user.Status = RecordStatuses.Inactive; await db.SaveChangesAsync(cancellationToken); return true;
    }
}
