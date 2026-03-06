using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

    public List<Book> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Books
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetCoverAsync(int id)
    {
        var path = await _db.Books
            .Where(b => b.Id == id)
            .Select(b => b.PdfPath)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            return NotFound();

        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/pdf");
    }
}
