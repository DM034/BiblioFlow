using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Loans;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

    public List<Row> Rows { get; set; } = [];

    public async Task OnGetAsync()
    {
        Rows = await _db.Loans
            .Include(l => l.Book)
            .OrderByDescending(l => l.StartAt)
            .Select(l => new Row
            {
                BookTitle = l.Book.Title,
                UserEmail = l.UserEmail,
                StartAt = l.StartAt,
                DueAt = l.DueAt,
                ReturnedAt = l.ReturnedAt
            })
            .ToListAsync();
    }

    public class Row
    {
        public string BookTitle { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public DateTime StartAt { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
    }
}
