using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Biblio.BackOffice.Data;
using Biblio.BackOffice.Models;

namespace Biblio.BackOffice.Pages.Admin.Books
{
    public class DetailsModel : PageModel
    {
        private readonly Biblio.BackOffice.Data.LibraryDbContext _context;

        public DetailsModel(Biblio.BackOffice.Data.LibraryDbContext context)
        {
            _context = context;
        }

        public Biblio.BackOffice.Data.Book Book { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id);

            if (book is not null)
            {
                Book = book;

                return Page();
            }

            return NotFound();
        }
    }
}
