using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Implementations;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add Entity Framework with optimizations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    // ✅ QuerySplittingBehavior configuré pour résoudre les erreurs de concurrence
    // et améliorer les performances sur les requêtes avec multiple collections
});

// Configuration des paramètres d'application
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Controllers for API endpoints
builder.Services.AddControllers();

// Add Radzen services
builder.Services.AddScoped<Radzen.DialogService>();
builder.Services.AddScoped<Radzen.NotificationService>();
builder.Services.AddScoped<Radzen.TooltipService>();
builder.Services.AddScoped<Radzen.ContextMenuService>();

// Injection des services métier
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IFileExplorerService, FileExplorerService>();
builder.Services.AddScoped<ITypeProduitService, TypeProduitService>();
builder.Services.AddScoped<ITypeDocumentImportService, TypeDocumentImportService>();
builder.Services.AddScoped<IFicheTechniqueService, FicheTechniqueService>();
builder.Services.AddScoped<IMemoireTechniqueService, MemoireTechniqueService>();
builder.Services.AddScoped<IDocumentGenereService, DocumentGenereService>();
builder.Services.AddScoped<ITypeSectionService, TypeSectionService>();
builder.Services.AddScoped<ISectionLibreService, SectionLibreService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IChantierService, ChantierService>();
builder.Services.AddScoped<ISectionConteneurService, SectionConteneurService>();
builder.Services.AddScoped<IFTConteneurService, FTConteneurService>();

// Nouveaux services Phase 2 - Architecture optimisée
builder.Services.AddScoped<IDocumentRepositoryService, DocumentRepositoryService>();
builder.Services.AddScoped<IDocumentExportService, DocumentExportService>();

// Service MemoryCache pour optimisations performance
builder.Services.AddMemoryCache();

// Service de cache centralisé Phase 3C
builder.Services.AddScoped<ICacheService, CacheService>();

// Service de gestion d'état de chargement Phase 3D
builder.Services.AddScoped<ILoadingStateService, LoadingStateService>();

// Service de protection anti-concurrence DbContext (CORRECTION CRITIQUE)
builder.Services.AddScoped<IOperationLockService, OperationLockService>();

// PDF Generation services
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddScoped<IHtmlTemplateService, HtmlTemplateService>();

var app = builder.Build();

// Initialize default types on startup
using (var scope = app.Services.CreateScope())
{
    var typeProduitService = scope.ServiceProvider.GetRequiredService<ITypeProduitService>();
    await typeProduitService.InitializeDefaultTypesAsync();
    
    var typeDocumentService = scope.ServiceProvider.GetRequiredService<ITypeDocumentImportService>();
    await typeDocumentService.InitializeDefaultTypesAsync();
    
    var typeSectionService = scope.ServiceProvider.GetRequiredService<ITypeSectionService>();
    await typeSectionService.InitializeDefaultTypesAsync();
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

// Configure static files for uploaded images
using (var scope = app.Services.CreateScope())
{
    var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
    var appSettings = await configService.GetAppSettingsAsync();
    
    if (!string.IsNullOrEmpty(appSettings.RepertoireStockageImages) && 
        Directory.Exists(appSettings.RepertoireStockageImages))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                appSettings.RepertoireStockageImages),
            RequestPath = "/images"
        });
    }
}

app.UseRouting();

// Map API controllers
app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
