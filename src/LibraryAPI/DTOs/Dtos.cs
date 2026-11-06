namespace LibraryAPI.DTOs;

public sealed record LoginRequestDto(string Username, string Password);
public sealed record RegisterRequestDto(string Username, string Password, string FullName);
public sealed record AuthResponseDto(int UserId, string Username, string Role, string FullName, string Token);
public sealed record CreateBookDto(string Title, string Author, string ISBN, int TotalCopies);
public sealed record UpdateBookDto(string Title, string Author, string ISBN, int TotalCopies);
public sealed record CreateUserDto(string Username, string Password, string FullName, string Role);
public sealed record UpdateUserDto(string FullName, string Role, string Status);
public sealed record BorrowRequestDto(List<int> BookCopyIds, DateTime ExpectedReturnDate);
public sealed record ReturnBookDto(DateTime ReturnDate);
public sealed record CreateCopyDto(int BookTitleId, string Barcode);
