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

        string root;
        try
        {
            root = ResolvePdfRoot();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"PDF storage is not accessible: {ex.Message}");
            Book = book;
            CurrentPath = book.PdfPath;
            return Page();
        }

        var target = Path.Combine(root, $"{book.Id}.pdf");
        try
        {
            await using var fs = System.IO.File.Create(target);
            await Pdf.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Upload failed: {ex.Message}");
            Book = book;
            CurrentPath = book.PdfPath;
            return Page();
        }

        book.PdfPath = target;
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Books");
    }

    private string ResolvePdfRoot()
    {
        var configured = _cfg["Storage:PdfRoot"];

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
            candidates.Add(Environment.ExpandEnvironmentVariables(configured));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            candidates.Add(Path.Combine(localAppData, "BiblioFlow", "pdfs"));

        candidates.Add(Path.Combine(Path.GetTempPath(), "biblioflow-pdfs"));

        foreach (var candidate in candidates)
        {
            try
            {
                var full = Path.GetFullPath(candidate);
                Directory.CreateDirectory(full);
                return full;
            }
            catch
            {
            }
        }

        throw new IOException("Unable to create a writable folder for PDF storage.");
    }
}
