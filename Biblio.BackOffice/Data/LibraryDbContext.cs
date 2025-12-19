using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Book>(e =>
        {
            e.HasIndex(x => x.Title);
            e.HasIndex(x => x.Author);
        });

        mb.Entity<License>(e =>
        {
            e.HasKey(x => x.BookId);
            e.HasOne(x => x.Book)
             .WithOne(x => x.License!)
             .HasForeignKey<License>(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Loan>(e =>
        {
            e.HasOne(x => x.Book)
             .WithMany(x => x.Loans)
             .HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.BookId, x.UserEmail, x.ReturnedAt, x.DueAt });
        });
    }
}
