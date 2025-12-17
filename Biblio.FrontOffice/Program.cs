using Biblio.FrontOffice.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SqlLibraryRepository>();

var app = builder.Build();

app.UseStaticFiles();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
