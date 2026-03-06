using Biblio.BackOffice.Data;
using Biblio.BackOffice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class EditModel : PageModel
{
    private readonly LibraryDbContext _db;
    private readonly IAdminAuditService _audit;

    public EditModel(LibraryDbContext db, IAdminAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty] public Book Book { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var b = await _db.Books.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        Book = b;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var b = await _db.Books.FirstOrDefaultAsync(x => x.Id == Book.Id);
        if (b == null) return NotFound();

        b.Title = Book.Title;
        b.Author = Book.Author;
        b.Category = Book.Category;
        b.Year = Book.Year;
        b.Summary = Book.Summary;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            action: "UPDATE",
            entityType: "Book",
            entityId: b.Id.ToString(),
            details: $"Title={b.Title}; Author={b.Author}");

        return Redirect("/Admin/Books");
    }
}
