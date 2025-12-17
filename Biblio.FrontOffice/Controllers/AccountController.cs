using Microsoft.AspNetCore.Mvc;

namespace Biblio.FrontOffice.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return View();
        HttpContext.Session.SetString("email", email.Trim());
        return RedirectToAction("Index", "Books");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("email");
        return RedirectToAction("Index", "Books");
    }
}
