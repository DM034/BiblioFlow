using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Biblio.BackOffice.Pages;

public class LoginModel : PageModel
{
    [BindProperty]
    public string? Email { get; set; }

    public string? Error { get; set; }

    public void OnGet()
    {
        Email = HttpContext.Session.GetString("admin_email") ?? "";
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Error = "Email is required.";
            return Page();
        }

        HttpContext.Session.SetString("admin_email", Email.Trim());
        TempData["Msg"] = "Logged in.";
        return Redirect("/Admin/Dashboard");
    }
}
