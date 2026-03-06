using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblio.BackOffice.Pages.Admin.Licenses;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    private readonly IAdminAuditService _audit;

    public IndexModel(LibraryDbContext db, IAdminAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public List<Row> Rows { get; set; } = new();

    public record Row(int BookId, string Title, int Seats);

    public async Task OnGetAsync()
    {
        Rows = await
            (from b in _db.Books
             join l in _db.Licenses on b.Id equals l.BookId into gj
             from l in gj.DefaultIfEmpty()
             orderby b.Title
             select new Row(
                 b.Id,
                 b.Title,
                 l == null ? 1 : l.ConcurrentSeats
             ))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(int bookId, int seats)
    {
        if (bookId <= 0)
            return RedirectToPage();

        var safeSeats = Math.Max(1, seats);
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        if (book == null)
            return RedirectToPage();

        var license = await _db.Licenses.FirstOrDefaultAsync(l => l.BookId == bookId);
        if (license == null)
        {
            _db.Licenses.Add(new License
            {
                BookId = bookId,
                ConcurrentSeats = safeSeats
            });
        }
        else
        {
            license.ConcurrentSeats = safeSeats;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(
            action: "UPDATE_SEATS",
            entityType: "License",
            entityId: bookId.ToString(),
            details: $"Title={book.Title}; Seats={safeSeats}");

        TempData["Msg"] = "License updated.";
        return RedirectToPage();
    }
}
