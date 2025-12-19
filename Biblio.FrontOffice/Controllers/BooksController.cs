using Microsoft.AspNetCore.Mvc;
using Biblio.FrontOffice.Data;

namespace Biblio.FrontOffice.Controllers;

public class BooksController : Controller
{
    private readonly SqlLibraryRepository _repo;
    public BooksController(SqlLibraryRepository repo) => _repo = repo;

    private string? Email => HttpContext.Session.GetString("email");

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null, bool onlyAvailable = false)
    {
        var (items, total) = await _repo.GetBooksAsync(page, pageSize, q, onlyAvailable);

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.Q = q ?? "";
        ViewBag.OnlyAvailable = onlyAvailable;
        ViewBag.Pages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Email = Email;

        return View(items);
    }

    public async Task<IActionResult> MyLoans()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return RedirectToAction("Login", "Account");

        var loans = await _repo.GetUserLoansAsync(Email!);
        ViewBag.Email = Email;
        return View(loans);
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

    [HttpPost]
    public async Task<IActionResult> ReturnByBook(int id)
    {
        if (string.IsNullOrWhiteSpace(Email))
            return RedirectToAction("Login", "Account");

        await _repo.ReturnByBookAsync(id, Email!);
        TempData["Msg"] = "Returned.";
        return RedirectToAction(nameof(MyLoans));
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
