using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Pages.Admin.Books;

public class ExportPdfModel : PageModel
{
    private readonly LibraryDbContext _db;
    public ExportPdfModel(LibraryDbContext db) => _db = db;

    public async Task<IActionResult> OnPostAsync()
    {
        var books = await _db.Books
            .Include(b => b.License)
            .OrderBy(b => b.Title)
            .ToListAsync();

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);

                page.Content().Column(col =>
                {
                    col.Item().Text("BiblioFlow — Catalogue").FontSize(18).SemiBold();
                    col.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(10);
                    col.Item().PaddingVertical(10).LineHorizontal(1);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.ConstantColumn(40);
                            c.ConstantColumn(50);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(CellStyle).Text("Title").SemiBold();
                            h.Cell().Element(CellStyle).Text("Author").SemiBold();
                            h.Cell().Element(CellStyle).Text("Category").SemiBold();
                            h.Cell().Element(CellStyle).Text("Year").SemiBold();
                            h.Cell().Element(CellStyle).Text("Seats").SemiBold();
                        });

                        foreach (var b in books)
                        {
                            t.Cell().Element(CellStyle).Text(b.Title);
                            t.Cell().Element(CellStyle).Text(b.Author);
                            t.Cell().Element(CellStyle).Text(b.Category);
                            t.Cell().Element(CellStyle).Text(b.Year?.ToString() ?? "");
                            t.Cell().Element(CellStyle).Text((b.License?.ConcurrentSeats ?? 1).ToString());
                        }
                    });
                });
            });
        }).GeneratePdf();

        return File(bytes, "application/pdf", "BiblioFlow-Catalogue.pdf");
    }

    public void OnGet() { }

    static IContainer CellStyle(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
