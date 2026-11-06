using LibraryAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BookTitle> BookTitles => Set<BookTitle>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<BorrowTicket> BorrowTickets => Set<BorrowTicket>();
    public DbSet<BorrowDetail> BorrowDetails => Set<BorrowDetail>();
    public DbSet<FineRecord> FineRecords => Set<FineRecord>();
    public DbSet<BorrowProcedureResult> BorrowProcedureResults => Set<BorrowProcedureResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
        });
        modelBuilder.Entity<BookTitle>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Author).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ISBN).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.ISBN).IsUnique();
            entity.HasMany(x => x.Copies).WithOne(x => x.BookTitle).HasForeignKey(x => x.BookTitleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Barcode).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Barcode).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
        });
        modelBuilder.Entity<BorrowTicket>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.User).WithMany(x => x.BorrowTickets).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Details).WithOne(x => x.BorrowTicket).HasForeignKey(x => x.BorrowTicketId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<BorrowDetail>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.BookCopy).WithMany(x => x.BorrowDetails).HasForeignKey(x => x.BookCopyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FineRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FineAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.BorrowTicket).WithMany(x => x.Fines).HasForeignKey(x => x.BorrowTicketId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany(x => x.Fines).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BorrowProcedureResult>().HasNoKey();
    }
}
