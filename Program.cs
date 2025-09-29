using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Implementations;
using GenerateurDOE.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add Entity Framework with DbContextFactory Pattern for optimal concurrency
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    switch (databaseProvider.ToUpper())
    {
        case "POSTGRESQL":
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                npgsqlOptions.CommandTimeout(300);
            })
            // ⚡ Configuration critique pour DateTime PostgreSQL
            .EnableServiceProviderCaching()
            .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());

            // 🔧 Configuration pour utiliser TIMESTAMP WITHOUT TIME ZONE par défaut
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            break;

        case "SQLSERVER":
        default:
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.CommandTimeout(300);
            });
            break;
    }

    // ✅ Multi-Database Support: PostgreSQL + SQL Server avec QuerySplittingBehavior optimisé
    // DbContextFactory Pattern: résolution définitive des problèmes de concurrence
});

// Configuration des paramètres d'application
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Controllers for API endpoints
builder.Services.AddControllers();

// Add HTTP Context Accessor for dynamic URL detection in PdfGenerationService
builder.Services.AddHttpContextAccessor();

// Add HttpClient for Blazor Server (required for API calls from Razor pages)
builder.Services.AddHttpClient();

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

// Add health checks for Docker container monitoring
builder.Services.AddHealthChecks();

// Configure DataProtection for Docker containers
// This ensures that antiforgery tokens and other protected data survive container restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/DataProtection-Keys"))
    .SetApplicationName("GenerateurDOE")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Keys valid for 90 days

// Service de suppression centralisé avec validation et audit
builder.Services.AddScoped<IDeletionService, DeletionService>();

// Service de téléchargement de documents factorisé
builder.Services.AddScoped<IDocumentDownloadService, DocumentDownloadService>();

// Service de sauvegarde complète (base de données + fichiers)
builder.Services.AddScoped<IBackupService, BackupService>();

// PDF Generation services
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddScoped<IHtmlTemplateService, HtmlTemplateService>();
builder.Services.AddScoped<IPdfProgressService, PdfProgressService>();
builder.Services.AddScoped<IPdfProgressDialogService, PdfProgressDialogService>();
builder.Services.AddScoped<IPdfPageCountService, PdfPageCountService>();

// Template management services
builder.Services.AddScoped<IPageGardeTemplateService, PageGardeTemplateService>();

// Configure Kestrel to use HTTP only - Let Docker handle port mapping
// Removed specific port binding to avoid conflicts with Docker port management

var app = builder.Build();

// Disable HTTPS redirection in production
if (app.Environment.IsProduction())
{
    // Skip HTTPS redirection for Docker deployment
}
else
{
    // Only use HTTPS redirection in development
    app.UseHttpsRedirection();
}

// Initialize database and default types on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Apply any pending database migrations
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var context = await contextFactory.CreateDbContextAsync();

        Log.Information("Applying database migrations...");
        await context.Database.MigrateAsync();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Log.Information("Database initialized successfully with {MigrationCount} migrations applied",
            appliedMigrations.Count());

        // Initialize default types
        var typeProduitService = scope.ServiceProvider.GetRequiredService<ITypeProduitService>();
        await typeProduitService.InitializeDefaultTypesAsync();

        var typeDocumentService = scope.ServiceProvider.GetRequiredService<ITypeDocumentImportService>();
        await typeDocumentService.InitializeDefaultTypesAsync();

        var typeSectionService = scope.ServiceProvider.GetRequiredService<ITypeSectionService>();
        await typeSectionService.InitializeDefaultTypesAsync();

        Log.Information("Default data initialization completed");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Critical error during database initialization");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// HTTPS redirection disabled - handled above based on environment

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

// Health check endpoint for Docker container monitoring
app.MapHealthChecks("/health");

app.Run();
