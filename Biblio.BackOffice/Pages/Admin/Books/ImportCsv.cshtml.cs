using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Models;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class ImportCsvModel : PageModel
{
    private readonly LibraryDbContext _db;
    public ImportCsvModel(LibraryDbContext db) => _db = db;

    [BindProperty] public IFormFile? CsvFile { get; set; }
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
            Delimiter = ","
        };

        int created = 0;

        using var stream = CsvFile.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, cfg);

        var rows = csv.GetRecords<BookCsvRow>().ToList();

        foreach (var r in rows)
        {
            var book = new Book
            {
                Title = r.Title?.Trim() ?? "",
                Author = r.Author?.Trim() ?? "",
                Category = r.Category?.Trim() ?? "",
                Year = r.Year,
                Summary = r.Summary?.Trim() ?? "",
                PdfPath = r.PdfPath?.Trim() ?? ""
            };

            _db.Books.Add(book);
            await _db.SaveChangesAsync(); // pour obtenir book.Id

            _db.Licenses.Add(new License
            {
                BookId = book.Id,
                ConcurrentSeats = r.ConcurrentSeats <= 0 ? 1 : r.ConcurrentSeats
            });

            created++;
        }

        await _db.SaveChangesAsync();
        Message = $"Imported: {created} book(s).";
        return Page();
    }

    public class BookCsvRow
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public int? Year { get; set; }
        public string? Summary { get; set; }
        public string? PdfPath { get; set; }
        public int ConcurrentSeats { get; set; } = 1;
    }
}
