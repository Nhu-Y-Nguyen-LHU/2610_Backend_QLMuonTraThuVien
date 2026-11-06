using LibraryAPI.DTOs;
using LibraryAPI.Entities;

namespace LibraryAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
}
public interface IBookService
{
    Task<IReadOnlyList<BookTitle>> SearchAsync(string? query, CancellationToken cancellationToken);
    Task<BookTitle?> GetAsync(int id, CancellationToken cancellationToken);
    Task<BookTitle> CreateAsync(CreateBookDto request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, UpdateBookDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<BookCopy> AddCopyAsync(CreateCopyDto request, CancellationToken cancellationToken);
}
public interface IBorrowService
{
    Task<int> BorrowAsync(int userId, BorrowRequestDto request, CancellationToken cancellationToken);
    Task<bool> ReturnAsync(int ticketId, DateTime returnDate, int requesterId, bool requesterIsStaff, CancellationToken cancellationToken);
    Task<IReadOnlyList<BorrowTicket>> GetTicketsAsync(int? userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FineRecord>> GetFinesAsync(int? userId, CancellationToken cancellationToken);
    Task<bool> PayFineAsync(int fineId, CancellationToken cancellationToken);
}
public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User?> GetAsync(int id, CancellationToken cancellationToken);
    Task<User> CreateAsync(CreateUserDto request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, UpdateUserDto request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
