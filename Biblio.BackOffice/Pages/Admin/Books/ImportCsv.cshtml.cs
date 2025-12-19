using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Models;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class ImportCsvModel : PageModel
{
    private readonly LibraryDbContext _db;
    public ImportCsvModel(LibraryDbContext db) => _db = db;

    [BindProperty]
    public IFormFile? CsvFile { get; set; }

    public string? Message { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CsvFile == null || CsvFile.Length == 0)
        {
            Message = "CSV file is required.";
            return Page();
        }

        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            BadDataFound = null
        };

        int created = 0, skipped = 0;

        using var stream = CsvFile.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, cfg);

        var rows = csv.GetRecords<Row>().ToList();

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Author))
            {
                skipped++;
                continue;
            }

            var exists = _db.Books.Any(b => b.Title == r.Title && b.Author == r.Author);
            if (exists) { skipped++; continue; }

            var book = new Biblio.BackOffice.Data.Book
            {
                Title = r.Title.Trim(),
                Author = r.Author.Trim(),
                Category = string.IsNullOrWhiteSpace(r.Category) ? "General" : r.Category.Trim(),
                Year = r.Year,
                Summary = string.IsNullOrWhiteSpace(r.Summary) ? null : r.Summary.Trim(),
                PdfPath = null
            };

            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            _db.Licenses.Add(new Biblio.BackOffice.Data.License
            {
                BookId = book.Id,
                ConcurrentSeats = r.ConcurrentSeats > 0 ? r.ConcurrentSeats : 1
            });

            await _db.SaveChangesAsync();
            created++;
        }

        Message = $"Imported: {created}\nSkipped: {skipped}";
        return Page();
    }

    public class Row
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string? Category { get; set; }
        public int? Year { get; set; }
        public string? Summary { get; set; }
        public int ConcurrentSeats { get; set; } = 1;
    }
}
