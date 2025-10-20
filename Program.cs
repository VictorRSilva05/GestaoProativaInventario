using GestaoProativaInventario.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<GestaoProativaInventario.Services.CsvImportService>();
builder.Services.AddScoped<GestaoProativaInventario.Services.CategoriaService>();
builder.Services.AddScoped<GestaoProativaInventario.Services.ProdutoService>();
builder.Services.AddScoped<GestaoProativaInventario.Services.PrevisaoService>();
builder.Services.AddScoped<GestaoProativaInventario.Services.AlertaService>();
builder.Services.AddScoped<GestaoProativaInventario.Services.DashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
