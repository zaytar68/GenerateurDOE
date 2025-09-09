using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration des paramètres d'application
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Injection des services métier
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IFileExplorerService, FileExplorerService>();
builder.Services.AddScoped<ITypeProduitService, TypeProduitService>();
builder.Services.AddScoped<ITypeDocumentImportService, TypeDocumentImportService>();
builder.Services.AddScoped<IFicheTechniqueService, FicheTechniqueService>();
builder.Services.AddScoped<IMemoireTechniqueService, MemoireTechniqueService>();
builder.Services.AddScoped<IDocumentExportService, DocumentExportService>();

var app = builder.Build();

// Initialize default types on startup
using (var scope = app.Services.CreateScope())
{
    var typeProduitService = scope.ServiceProvider.GetRequiredService<ITypeProduitService>();
    await typeProduitService.InitializeDefaultTypesAsync();
    
    var typeDocumentService = scope.ServiceProvider.GetRequiredService<ITypeDocumentImportService>();
    await typeDocumentService.InitializeDefaultTypesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
