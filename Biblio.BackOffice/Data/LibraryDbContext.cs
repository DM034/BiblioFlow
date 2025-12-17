using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Models;

namespace Biblio.BackOffice.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) {}

    public DbSet<Book> Books => Set<Book>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().HasIndex(b => b.Title);
        modelBuilder.Entity<Book>().HasIndex(b => b.Author);

        modelBuilder.Entity<License>().HasKey(l => l.BookId);
        
        modelBuilder.Entity<License>()
            .HasOne(l => l.Book)
            .WithOne(b => b.License)
            .HasForeignKey<License>(l => l.BookId);

        modelBuilder.Entity<Loan>()
            .HasIndex(l => new { l.BookId, l.UserEmail, l.ReturnedAt, l.DueAt });
    }
}
