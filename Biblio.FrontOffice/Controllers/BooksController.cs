using Microsoft.AspNetCore.Mvc;
using Biblio.FrontOffice.Data;

namespace Biblio.FrontOffice.Controllers;

public class BooksController : Controller
{
    private readonly SqlLibraryRepository _repo;
    public BooksController(SqlLibraryRepository repo) => _repo = repo;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null)
    {
        var (items, total) = await _repo.GetBooksAsync(page, pageSize, q);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.Q = q;
        return View(items);
    }
}
