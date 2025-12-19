using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Models;

namespace Biblio.BackOffice.Pages.Admin.Books
{
    public class CreateModel : PageModel
    {
        private readonly Biblio.BackOffice.Data.LibraryDbContext _context;

        public CreateModel(Biblio.BackOffice.Data.LibraryDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Book Book { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Books.Add(Book);
            await _context.SaveChangesAsync();

            _context.Licenses.Add(new Biblio.BackOffice.Models.License
            {
                BookId = Book.Id,
                ConcurrentSeats = 1
            });
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
