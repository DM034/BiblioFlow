using System.Text;
using System.Text.Json;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblio.BackOffice.Pages.Admin.Import;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    private readonly IAdminAuditService _audit;

    public IndexModel(LibraryDbContext db, IAdminAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty] public IFormFile? Csv { get; set; }
    [BindProperty] public string? PreviewPayload { get; set; }

    public string? Report { get; set; }
    public List<PreviewRow> PreviewRows { get; set; } = [];
    public int PreviewValidCount { get; set; }
    public int PreviewInvalidCount { get; set; }

    public void OnGet() { }

    public Task<IActionResult> OnPostAsync() => OnPostPreviewAsync();

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (Csv == null || Csv.Length == 0)
        {
            Report = "No file.";
            return Page();
        }

        var parseResult = await ParseCsvAsync(Csv.OpenReadStream());
        PreviewRows = parseResult.Rows;
        PreviewValidCount = parseResult.ValidRows.Count;
        PreviewInvalidCount = parseResult.Rows.Count - parseResult.ValidRows.Count;
        PreviewPayload = JsonSerializer.Serialize(parseResult.ValidRows);

        if (parseResult.Rows.Count == 0)
        {
            Report = "CSV contains no data rows.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (string.IsNullOrWhiteSpace(PreviewPayload))
        {
            Report = "No preview data. Upload a CSV then click Preview.";
            return Page();
        }

        List<ImportRow> rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<ImportRow>>(PreviewPayload) ?? [];
        }
        catch
        {
            Report = "Invalid preview payload. Please preview the CSV again.";
            return Page();
        }

        if (rows.Count == 0)
        {
            Report = "Nothing to import (no valid rows in preview).";
            return Page();
        }

        var sb = new StringBuilder();
        int created = 0, updated = 0, failed = 0;

        foreach (var row in rows)
        {
            try
            {
                var book = await _db.Books.FirstOrDefaultAsync(b => b.Title == row.Title && b.Author == row.Author);
                if (book == null)
                {
                    book = new Book
                    {
                        Title = row.Title,
                        Author = row.Author,
                        Category = row.Category,
                        Year = row.Year,
                        Summary = row.Summary,
                        PdfPath = null
                    };

                    _db.Books.Add(book);
                    await _db.SaveChangesAsync();
                    created++;
                }
                else
                {
                    book.Category = row.Category;
                    book.Year = row.Year;
                    book.Summary = row.Summary;
                    updated++;
                }

                var lic = await _db.Licenses.FirstOrDefaultAsync(l => l.BookId == book.Id);
                if (lic == null)
                {
                    _db.Licenses.Add(new License { BookId = book.Id, ConcurrentSeats = row.Seats });
                }
                else
                {
                    lic.ConcurrentSeats = row.Seats;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                failed++;
                sb.AppendLine($"Line {row.LineNo}: {ex.Message}");
            }
        }

        Report = $"Imported rows={rows.Count} | Created={created}, Updated={updated}, Failed={failed}\n\n{sb}";

        await _audit.LogAsync(
            action: "IMPORT_CSV",
            entityType: "Book",
            details: $"Rows={rows.Count}; Created={created}; Updated={updated}; Failed={failed}");

        PreviewRows = [];
        PreviewPayload = null;
        PreviewValidCount = 0;
        PreviewInvalidCount = 0;

        return Page();
    }

    private async Task<ParseResult> ParseCsvAsync(Stream stream)
    {
        var rows = new List<PreviewRow>();
        var validRows = new List<ImportRow>();

        using var sr = new StreamReader(stream);
        var header = await sr.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header))
            return new ParseResult(rows, validRows);

        var sep = header.Contains(';') ? ';' : ',';

        var existingBooks = await _db.Books
            .Select(b => new { b.Title, b.Author })
            .ToListAsync();

        var existingKeys = existingBooks
            .Select(x => BuildBookKey(x.Title, x.Author))
            .ToHashSet();

        int lineNo = 1;
        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(sep);
            if (parts.Length < 6)
            {
                rows.Add(new PreviewRow(lineNo, "", "", "", null, "", 1, false, false, "Invalid columns"));
                continue;
            }

            var title = parts[0].Trim();
            var author = parts[1].Trim();
            var category = string.IsNullOrWhiteSpace(parts[2]) ? "General" : parts[2].Trim();
            int? year = int.TryParse(parts[3].Trim(), out var y) ? y : null;
            var summary = string.IsNullOrWhiteSpace(parts[4]) ? null : parts[4].Trim();
            var seats = int.TryParse(parts[5].Trim(), out var s) ? Math.Max(1, s) : 1;

            var validationError = ValidateRow(title, author);
            var exists = existingKeys.Contains(BuildBookKey(title, author));

            if (validationError != null)
            {
                rows.Add(new PreviewRow(lineNo, title, author, category, year, summary, seats, false, exists, validationError));
                continue;
            }

            rows.Add(new PreviewRow(lineNo, title, author, category, year, summary, seats, true, exists, null));
            validRows.Add(new ImportRow(lineNo, title, author, category, year, summary, seats));
        }

        return new ParseResult(rows, validRows);
    }

    private static string BuildBookKey(string title, string author)
        => $"{title.Trim().ToLowerInvariant()}||{author.Trim().ToLowerInvariant()}";

    private static string? ValidateRow(string title, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";

        if (string.IsNullOrWhiteSpace(author))
            return "Author is required";

        return null;
    }

    private sealed record ParseResult(List<PreviewRow> Rows, List<ImportRow> ValidRows);

    public sealed record PreviewRow(
        int LineNo,
        string Title,
        string Author,
        string Category,
        int? Year,
        string? Summary,
        int Seats,
        bool IsValid,
        bool Exists,
        string? Error);

    public sealed record ImportRow(
        int LineNo,
        string Title,
        string Author,
        string Category,
        int? Year,
        string? Summary,
        int Seats);
}
