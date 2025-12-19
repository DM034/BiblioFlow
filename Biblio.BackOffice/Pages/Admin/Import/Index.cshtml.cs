using System.Text;
using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Import;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;

    public IndexModel(LibraryDbContext db) => _db = db;

    [BindProperty] public IFormFile? Csv { get; set; }
    public string? Report { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Csv == null || Csv.Length == 0)
        {
            Report = "No file.";
            return Page();
        }

        var sb = new StringBuilder();
        int ok = 0, ko = 0;

        using var sr = new StreamReader(Csv.OpenReadStream());
        var header = await sr.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header))
        {
            Report = "Empty CSV.";
            return Page();
        }

        var sep = header.Contains(';') ? ';' : ',';

        int lineNo = 1;
        while (!sr.EndOfStream)
        {
            lineNo++;
            var line = await sr.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(sep);
            if (parts.Length < 6)
            {
                ko++;
                sb.AppendLine($"Line {lineNo}: invalid columns");
                continue;
            }

            try
            {
                var title = parts[0].Trim();
                var author = parts[1].Trim();
                var category = parts[2].Trim();
                int? year = int.TryParse(parts[3].Trim(), out var y) ? y : null;
                var summary = parts[4].Trim();
                var seats = int.TryParse(parts[5].Trim(), out var s) ? Math.Max(1, s) : 1;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
                    throw new Exception("Title/Author required");

                var book = await _db.Books.FirstOrDefaultAsync(b => b.Title == title && b.Author == author);
                if (book == null)
                {
                    book = new Book
                    {
                        Title = title,
                        Author = author,
                        Category = category,
                        Year = year,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                        PdfPath = null
                    };
                    _db.Books.Add(book);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    book.Category = category;
                    book.Year = year;
                    book.Summary = string.IsNullOrWhiteSpace(summary) ? null : summary;
                }

                var lic = await _db.Licenses.FirstOrDefaultAsync(l => l.BookId == book.Id);
                if (lic == null)
                {
                    _db.Licenses.Add(new License { BookId = book.Id, ConcurrentSeats = seats });
                }
                else
                {
                    lic.ConcurrentSeats = seats;
                }

                await _db.SaveChangesAsync();
                ok++;
            }
            catch (Exception ex)
            {
                ko++;
                sb.AppendLine($"Line {lineNo}: {ex.Message}");
            }
        }

        Report = $"Imported OK={ok}, KO={ko}\n\n{sb}";
        return Page();
    }
}
