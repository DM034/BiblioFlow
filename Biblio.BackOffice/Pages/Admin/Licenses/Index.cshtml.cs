using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Licenses;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

    public List<Row> Rows { get; set; } = [];
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        Rows = await _db.Books
            .Include(b => b.License)
            .OrderBy(b => b.Title)
            .Select(b => new Row
            {
                BookId = b.Id,
                BookTitle = b.Title,
                Seats = b.License != null ? b.License.ConcurrentSeats : 1
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(int bookId, int seats)
    {
        if (seats < 1) seats = 1;

        var lic = await _db.Licenses.FirstOrDefaultAsync(l => l.BookId == bookId);
        if (lic == null)
        {
            _db.Licenses.Add(new Biblio.BackOffice.Models.License { BookId = bookId, ConcurrentSeats = seats });
        }
        else
        {
            lic.ConcurrentSeats = seats;
        }

        await _db.SaveChangesAsync();
        Message = "Saved.";
        await OnGetAsync();
        return Page();
    }

    public class Row
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public int Seats { get; set; }
    }
}
