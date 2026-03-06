using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Book>(e =>
        {
            e.HasIndex(x => x.Title);
            e.HasIndex(x => x.Author);
            e.HasIndex(x => new { x.Title, x.Author });
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
            e.HasIndex(x => new { x.UserEmail, x.ReturnedAt, x.DueAt });
            e.HasIndex(x => x.DueAt);
        });

        mb.Entity<AdminAuditEvent>(e =>
        {
            e.Property(x => x.AdminEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.Details).HasMaxLength(4000);
            e.Property(x => x.IpAddress).HasMaxLength(64);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.AdminEmail);
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
        });
    }
}
