using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

public sealed class BookService(AppDbContext db) : IBookService
{
    public async Task<IReadOnlyList<BookTitle>> SearchAsync(string? query, CancellationToken cancellationToken) =>
        await db.BookTitles.Include(x => x.Copies).Where(x => string.IsNullOrWhiteSpace(query) || x.Title.Contains(query) || x.Author.Contains(query) || x.ISBN.Contains(query)).AsNoTracking().ToListAsync(cancellationToken);
    public Task<BookTitle?> GetAsync(int id, CancellationToken cancellationToken) => db.BookTitles.Include(x => x.Copies).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<BookTitle> CreateAsync(CreateBookDto request, CancellationToken cancellationToken)
    {
        if (await db.BookTitles.AnyAsync(x => x.ISBN == request.ISBN, cancellationToken)) throw new InvalidOperationException("ISBN already exists.");
        var entity = new BookTitle { Title = request.Title, Author = request.Author, ISBN = request.ISBN, TotalCopies = request.TotalCopies };
        db.BookTitles.Add(entity); await db.SaveChangesAsync(cancellationToken); return entity;
    }
    public async Task<bool> UpdateAsync(int id, UpdateBookDto request, CancellationToken cancellationToken)
    {
        var entity = await db.BookTitles.FindAsync([id], cancellationToken);
        if (entity is null) return false;
        if (await db.BookTitles.AnyAsync(x => x.ISBN == request.ISBN && x.Id != id, cancellationToken)) throw new InvalidOperationException("ISBN already exists.");
        var copyCount = await db.BookCopies.CountAsync(x => x.BookTitleId == id, cancellationToken);
        if (request.TotalCopies < copyCount) throw new InvalidOperationException($"TotalCopies cannot be less than existing copies ({copyCount}).");
        entity.Title = request.Title; entity.Author = request.Author; entity.ISBN = request.ISBN; entity.TotalCopies = request.TotalCopies;
        await db.SaveChangesAsync(cancellationToken); return true;
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await db.BookTitles.FindAsync([id], cancellationToken); if (entity is null) return false;
        if (await db.BookCopies.AnyAsync(x => x.BookTitleId == id, cancellationToken))
            throw new InvalidOperationException("Cannot delete a book title while physical copies exist.");
        db.BookTitles.Remove(entity); await db.SaveChangesAsync(cancellationToken); return true;
    }
    public async Task<BookCopy> AddCopyAsync(CreateCopyDto request, CancellationToken cancellationToken)
    {
        var title = await db.BookTitles.FindAsync([request.BookTitleId], cancellationToken);
        if (title is null) throw new KeyNotFoundException("Book title not found.");
        if (await db.BookCopies.AnyAsync(x => x.Barcode == request.Barcode, cancellationToken)) throw new InvalidOperationException("Barcode already exists.");
        var copyCount = await db.BookCopies.CountAsync(x => x.BookTitleId == request.BookTitleId, cancellationToken);
        if (copyCount >= title.TotalCopies) throw new InvalidOperationException("The configured total copy limit has been reached.");
        var copy = new BookCopy { BookTitleId = request.BookTitleId, Barcode = request.Barcode };
        db.BookCopies.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return copy;
    }
}
