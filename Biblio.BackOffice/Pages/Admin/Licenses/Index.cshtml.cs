using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Licenses;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

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
}
