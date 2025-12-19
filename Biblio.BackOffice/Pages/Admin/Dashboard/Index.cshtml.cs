using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Dashboard;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

    public int TotalBooks { get; set; }
    public int NoPdf { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }

    public List<(string Title, int Count)> TopBorrowed { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalBooks = await _db.Books.CountAsync();
        NoPdf = await _db.Books.CountAsync(b => b.PdfPath == null || b.PdfPath == "");
        ActiveLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && DateTime.UtcNow <= l.DueAt);
        OverdueLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && DateTime.UtcNow > l.DueAt);

        var top = await _db.Loans
            .GroupBy(l => l.BookId)
            .Select(g => new { BookId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(5)
            .ToListAsync();

        foreach (var t in top)
        {
            var title = await _db.Books.Where(b => b.Id == t.BookId).Select(b => b.Title).FirstOrDefaultAsync() ?? $"Book#{t.BookId}";
            TopBorrowed.Add((title, t.Cnt));
        }
    }
}
