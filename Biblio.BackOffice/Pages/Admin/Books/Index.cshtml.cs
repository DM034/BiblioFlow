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
    public class IndexModel : PageModel
    {
        private readonly Biblio.BackOffice.Data.LibraryDbContext _context;

        public IndexModel(Biblio.BackOffice.Data.LibraryDbContext context)
        {
            _context = context;
        }

        public IList<Book> Book { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Book = await _context.Books.ToListAsync();
        }
    }
}
