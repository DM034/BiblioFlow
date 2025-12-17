using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class UploadPdfModel : PageModel
{
    private readonly LibraryDbContext _db;
    public UploadPdfModel(LibraryDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int BookId { get; set; }

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public string? Message { get; set; }

    // dossier où on stocke les PDF (accessible aussi par FrontOffice)
    private const string PdfRoot = "/home/dm/biblioflow/pdfs";

    public async Task<IActionResult> OnGetAsync()
    {
        var book = await _db.Books.FindAsync(BookId);
        if (book == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == BookId);
        if (book == null) return NotFound();

        if (PdfFile == null || PdfFile.Length == 0)
        {
            Message = "PDF is required.";
            return Page();
        }

        Directory.CreateDirectory(PdfRoot);

        var safeName = Path.GetFileNameWithoutExtension(PdfFile.FileName);
        var fileName = $"{BookId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{safeName}.pdf";
        var fullPath = Path.Combine(PdfRoot, fileName);

        await using (var fs = System.IO.File.Create(fullPath))
            await PdfFile.CopyToAsync(fs);

        book.PdfPath = fullPath;
        await _db.SaveChangesAsync();

        Message = $"Uploaded: {fullPath}";
        return Page();
    }
}
