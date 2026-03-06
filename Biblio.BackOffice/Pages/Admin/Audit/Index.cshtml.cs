using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Audit;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;

    public IndexModel(LibraryDbContext db)
    {
        _db = db;
    }

    public List<AdminAuditEvent> Rows { get; set; } = [];

    public async Task OnGetAsync()
    {
        Rows = await _db.AdminAuditEvents
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync();
    }
}
