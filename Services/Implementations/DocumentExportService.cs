using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.Extensions.Options;
using Markdig;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service d'export de documents optimisé pour le format PDF uniquement
/// Simplification de l'architecture Strategy Pattern pour améliorer les performances
/// </summary>
public class DocumentExportService : IDocumentExportService
{
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IHtmlTemplateService _htmlTemplateService;
    private readonly AppSettings _appSettings;

    public DocumentExportService(IOptions<AppSettings> appSettings,
        IHtmlTemplateService htmlTemplateService,
        IPdfGenerationService pdfGenerationService)
    {
        _appSettings = appSettings.Value;
        _htmlTemplateService = htmlTemplateService;
        _pdfGenerationService = pdfGenerationService;
    }

    public async Task<string> ExportContentAsync(string content, FormatExport format)
    {
        // Vérification : seul PDF est supporté
        if (format != FormatExport.PDF)
        {
            throw new NotSupportedException($"Seul le format PDF est supporté. Format demandé : {format}");
        }

        var options = new ExportOptions
        {
            FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}",
            SaveToFile = false,
            OutputDirectory = _appSettings.RepertoireStockagePDF
        };

        var result = await ExportWithMetadataAsync(content, format, options);
        return result.Content;
    }

    public async Task<ExportResult> ExportWithMetadataAsync(string content, FormatExport format, ExportOptions options)
    {
        // Vérification : seul PDF est supporté
        if (format != FormatExport.PDF)
        {
            throw new NotSupportedException($"Seul le format PDF est supporté. Format demandé : {format}");
        }

        var startTime = DateTime.Now;

        // Génération PDF directe sans Strategy Pattern
        var result = await GeneratePdfDirectAsync(content, options);
        result.GenerationTime = DateTime.Now - startTime;

        return result;
    }

    /// <summary>
    /// Génération PDF optimisée sans couche d'abstraction supplémentaire
    /// </summary>
    private async Task<ExportResult> GeneratePdfDirectAsync(string content, ExportOptions options)
    {
        // Convertir le Markdown en HTML avec Markdig
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var htmlContent = Markdown.ToHtml(content, pipeline);

        // Créer un document HTML complet avec CSS par défaut
        var completeHtml = $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Document Généré</title>
    <style>
        {_htmlTemplateService.GetDefaultDocumentCSS()}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";

        // Générer le PDF avec les API existantes
        var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(
            completeHtml,
            new PdfGenerationOptions
            {
                Format = "A4",
                DisplayHeaderFooter = true,
                MarginTop = "1cm",
                MarginBottom = "1cm",
                MarginLeft = "1cm",
                MarginRight = "1cm"
            });

        return new ExportResult
        {
            Content = Convert.ToBase64String(pdfBytes),
            MimeType = "application/pdf",
            FilePath = options.SaveToFile ?
                Path.Combine(options.OutputDirectory, $"{options.FileName}.pdf") : string.Empty,
            FileSize = pdfBytes.Length
        };
    }
}

