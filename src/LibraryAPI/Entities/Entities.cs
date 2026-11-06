using System.Text.Json.Serialization;

namespace LibraryAPI.Entities;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Librarian = "Librarian";
    public const string Reader = "Reader";
}

public static class RecordStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Available = "Available";
    public const string Borrowed = "Borrowed";
    public const string Lost = "Lost";
    public const string Open = "Open";
    public const string Returned = "Returned";
}

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Reader;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = RecordStatuses.Active;
    public ICollection<BorrowTicket> BorrowTickets { get; set; } = [];
    public ICollection<FineRecord> Fines { get; set; } = [];
}

public sealed class BookTitle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public ICollection<BookCopy> Copies { get; set; } = [];
}

public sealed class BookCopy
{
    public int Id { get; set; }
    public int BookTitleId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = RecordStatuses.Available;
    public BookTitle BookTitle { get; set; } = null!;
    public ICollection<BorrowDetail> BorrowDetails { get; set; } = [];
}

public sealed class BorrowTicket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string Status { get; set; } = RecordStatuses.Open;
    public User User { get; set; } = null!;
    public ICollection<BorrowDetail> Details { get; set; } = [];
    public ICollection<FineRecord> Fines { get; set; } = [];
}

public sealed class BorrowDetail
{
    public int Id { get; set; }
    public int BorrowTicketId { get; set; }
    public int BookCopyId { get; set; }
    public BorrowTicket BorrowTicket { get; set; } = null!;
    public BookCopy BookCopy { get; set; } = null!;
}

public sealed class FineRecord
{
    public int Id { get; set; }
    public int BorrowTicketId { get; set; }
    public int UserId { get; set; }
    public decimal FineAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public BorrowTicket BorrowTicket { get; set; } = null!;
    public User User { get; set; } = null!;
}
