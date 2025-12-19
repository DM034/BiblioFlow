using Biblio.BackOffice.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorPages();
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddDbContext<LibraryDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Guard /Admin/*
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/Admin"))
    {
        var admin = ctx.Session.GetString("admin_email");
        if (string.IsNullOrWhiteSpace(admin))
        {
            ctx.Response.Redirect("/Login");
            return;
        }
    }
    await next();
});

app.MapRazorPages();
app.Run();
