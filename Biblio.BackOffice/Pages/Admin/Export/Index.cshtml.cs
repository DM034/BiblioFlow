using Biblio.BackOffice.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Biblio.BackOffice.Pages.Admin.Export;

public class IndexModel : PageModel
{
    private readonly LibraryDbContext _db;
    public IndexModel(LibraryDbContext db) => _db = db;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var totalBooks = await _db.Books.CountAsync();
        var noPdf = await _db.Books.CountAsync(b => b.PdfPath == null || b.PdfPath == "");
        var activeLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && DateTime.UtcNow <= l.DueAt);
        var overdueLoans = await _db.Loans.CountAsync(l => l.ReturnedAt == null && DateTime.UtcNow > l.DueAt);

        var top = await _db.Loans
            .GroupBy(l => l.BookId)
            .Select(g => new { BookId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(5)
            .ToListAsync();

        var topTitles = new List<(string Title, int Cnt)>();
        foreach (var t in top)
        {
            var title = await _db.Books.Where(b => b.Id == t.BookId).Select(b => b.Title).FirstOrDefaultAsync() ?? $"Book#{t.BookId}";
            topTitles.Add((title, t.Cnt));
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Text("BiblioFlow — Report").SemiBold().FontSize(18);

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Total books: {totalBooks}");
                    col.Item().Text($"Books without PDF: {noPdf}");
                    col.Item().Text($"Active loans: {activeLoans}");
                    col.Item().Text($"Overdue loans: {overdueLoans}");

                    col.Item().PaddingTop(10).Text("Top borrowed").SemiBold();
                    foreach (var x in topTitles)
                        col.Item().Text($"- {x.Title} ({x.Cnt})");
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated at ");
                    x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", "report.pdf");
    }
}
