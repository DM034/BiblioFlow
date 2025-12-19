using Biblio.BackOffice.Data;
using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class UploadPdfModel : PageModel
{
    private readonly LibraryDbContext _db;
    private readonly IConfiguration _cfg;

    public UploadPdfModel(LibraryDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public IFormFile? Pdf { get; set; }

    public Book? Book { get; set; }
    public string? CurrentPath { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Book = await _db.Books.FirstOrDefaultAsync(b => b.Id == Id);
        if (Book == null) return NotFound();
        CurrentPath = Book.PdfPath;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == Id);
        if (book == null) return NotFound();

        if (Pdf == null || Pdf.Length == 0)
        {
            ModelState.AddModelError("", "Select a PDF.");
            Book = book;
            CurrentPath = book.PdfPath;
            return Page();
        }

        var root = _cfg["Storage:PdfRoot"] ?? "/tmp/biblioflow-pdfs";
        Directory.CreateDirectory(root);

        var target = Path.Combine(root, $"{book.Id}.pdf");
        await using (var fs = System.IO.File.Create(target))
            await Pdf.CopyToAsync(fs);

        book.PdfPath = target;
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Books");
    }
}
