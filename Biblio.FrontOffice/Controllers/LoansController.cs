using Microsoft.AspNetCore.Mvc;
using Biblio.FrontOffice.Data;

namespace Biblio.FrontOffice.Controllers;

public class LoansController : Controller
{
    private readonly SqlLibraryRepository _repo;
    public LoansController(SqlLibraryRepository repo) => _repo = repo;

    private string? Email => HttpContext.Session.GetString("email");

    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return RedirectToAction("Login", "Account");

        var items = await _repo.GetUserLoansAsync(Email!);
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Return(int loanId)
    {
        if (string.IsNullOrWhiteSpace(Email))
            return RedirectToAction("Login", "Account");

        await _repo.ReturnAsync(loanId, Email!);
        TempData["Msg"] = "Returned.";
        return RedirectToAction(nameof(Index));
    }
}
