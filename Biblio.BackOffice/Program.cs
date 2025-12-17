using Biblio.BackOffice.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<LibraryDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    db.Database.Migrate();

    var booksWithoutLicense = db.Books
        .Where(b => b.License == null)
        .Select(b => b.Id)
        .ToList();

    foreach (var id in booksWithoutLicense)
        db.Licenses.Add(new Biblio.BackOffice.Models.License { BookId = id, ConcurrentSeats = 1 });

    if (booksWithoutLicense.Count > 0)
        db.SaveChanges();
}

app.MapControllers();
app.MapDefaultControllerRoute();
app.UseStaticFiles();
app.MapRazorPages();
app.Run();
