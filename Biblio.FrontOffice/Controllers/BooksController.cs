using Microsoft.AspNetCore.Mvc;
using Biblio.FrontOffice.Data;

namespace Biblio.FrontOffice.Controllers;

public class BooksController : Controller
{
    private readonly SqlLibraryRepository _repo;
    public BooksController(SqlLibraryRepository repo) => _repo = repo;

    private string? Email => HttpContext.Session.GetString("email");

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null)
    {
        var (items, total) = await _repo.GetBooksAsync(page, pageSize, q);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.Q = q;
        ViewBag.Email = Email;
        ViewBag.Msg = TempData["Msg"] as string;
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Borrow(int id)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            TempData["Msg"] = "Please login first.";
            return RedirectToAction(nameof(Index));
        }

        var loanId = await _repo.BorrowAsync(id, Email!, days: 14);
        TempData["Msg"] = loanId == null ? "No seat available." : $"Borrowed (loanId={loanId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Read(int id)
    {
        if (string.IsNullOrWhiteSpace(Email)) return RedirectToAction("Login", "Account");

        if (!await _repo.HasActiveLoanAsync(id, Email!)) return Forbid();

        var path = await _repo.GetPdfPathAsync(id);
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return NotFound("PDF not found");

        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/pdf", enableRangeProcessing: true);
    }
}
