using Biblio.BackOffice.Data;
using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class EditModel : PageModel
{
    private readonly LibraryDbContext _db;
    public EditModel(LibraryDbContext db) => _db = db;

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
        return Redirect("/Admin/Books");
    }
}
