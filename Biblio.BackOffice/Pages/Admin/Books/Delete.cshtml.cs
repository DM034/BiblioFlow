using Biblio.BackOffice.Data;
using Biblio.BackOffice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class DeleteModel : PageModel
{
    private readonly LibraryDbContext _context;
    private readonly IAdminAuditService _audit;

    public DeleteModel(LibraryDbContext context, IAdminAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    [BindProperty]
    public Book Book { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id.Value);
        if (book == null)
            return NotFound();

        Book = book;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var book = await _context.Books.FindAsync(id.Value);
        if (book != null)
        {
            var details = $"Title={book.Title}; Author={book.Author}";
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("DELETE", "Book", id.Value.ToString(), details);
        }

        return RedirectToPage("./Index");
    }
}
