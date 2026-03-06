using Biblio.BackOffice.Data;
using Biblio.BackOffice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class CreateModel : PageModel
{
    private readonly LibraryDbContext _db;
    private readonly IAdminAuditService _audit;

    public CreateModel(LibraryDbContext db, IAdminAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty] public Book Book { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Book.Title) || string.IsNullOrWhiteSpace(Book.Author))
            return Page();

        _db.Books.Add(Book);
        await _db.SaveChangesAsync();

        // default license
        if (!await _db.Licenses.AnyAsync(l => l.BookId == Book.Id))
        {
            _db.Licenses.Add(new License { BookId = Book.Id, ConcurrentSeats = 1 });
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(
            action: "CREATE",
            entityType: "Book",
            entityId: Book.Id.ToString(),
            details: $"Title={Book.Title}; Author={Book.Author}");

        return Redirect("/Admin/Books");
    }
}
