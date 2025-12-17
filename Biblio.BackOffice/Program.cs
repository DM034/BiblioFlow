using Biblio.BackOffice.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddRazorPages();
builder.Services.AddDbContext<LibraryDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    db.Database.Migrate();

    var ids = db.Books.Where(b => b.License == null).Select(b => b.Id).ToList();
    foreach (var id in ids)
        db.Licenses.Add(new Biblio.BackOffice.Models.License { BookId = id, ConcurrentSeats = 1 });

    if (ids.Count > 0) db.SaveChanges();
}

app.Run();
