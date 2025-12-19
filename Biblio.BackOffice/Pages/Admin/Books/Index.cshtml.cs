using Microsoft.AspNetCore.Mvc.RazorPages;
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
}
