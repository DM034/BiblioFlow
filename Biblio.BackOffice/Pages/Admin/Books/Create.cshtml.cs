using Biblio.BackOffice.Data;
using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class CreateModel : PageModel
{
    private readonly LibraryDbContext _db;
    public CreateModel(LibraryDbContext db) => _db = db;

    [BindProperty] public Book Book { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Book.Title) || string.IsNullOrWhiteSpace(Book.Author))
            return Page();

        _db.Books.Add(Book);
        await _db.SaveChangesAsync();

        // default license
        if (!_db.Licenses.Any(l => l.BookId == Book.Id))
        {
            _db.Licenses.Add(new License { BookId = Book.Id, ConcurrentSeats = 1 });
            await _db.SaveChangesAsync();
        }

        return Redirect("/Admin/Books");
    }
}
