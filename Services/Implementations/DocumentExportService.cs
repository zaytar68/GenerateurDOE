using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.Extensions.Options;
using Markdig;

namespace GenerateurDOE.Services.Implementations;

public class DocumentExportService : IDocumentExportService
{
    private readonly Dictionary<FormatExport, IExportStrategy> _strategies;
    private readonly AppSettings _appSettings;

    public DocumentExportService(IOptions<AppSettings> appSettings, 
        IHtmlTemplateService htmlTemplateService,
        IPdfGenerationService pdfGenerationService)
    {
        _appSettings = appSettings.Value;
        
        // Initialiser les stratégies d'export
        _strategies = new Dictionary<FormatExport, IExportStrategy>
        {
            { FormatExport.HTML, new HtmlExportStrategy(htmlTemplateService) },
            { FormatExport.Markdown, new MarkdownExportStrategy() },
            { FormatExport.PDF, new PdfExportStrategy(pdfGenerationService, htmlTemplateService, _appSettings) },
            { FormatExport.Word, new WordExportStrategy() }
        };
    }

    public async Task<string> ExportContentAsync(string content, FormatExport format)
    {
        var options = new ExportOptions
        {
            FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}",
            SaveToFile = false,
            OutputDirectory = GetOutputDirectory(format)
        };

        var result = await ExportWithMetadataAsync(content, format, options);
        return result.Content;
    }

    public async Task<ExportResult> ExportWithMetadataAsync(string content, FormatExport format, ExportOptions options)
    {
        if (!_strategies.TryGetValue(format, out var strategy))
        {
            throw new NotSupportedException($"Format d'export non supporté : {format}");
        }

        var startTime = DateTime.Now;
        var result = await strategy.ExportAsync(content, options);
        result.GenerationTime = DateTime.Now - startTime;

        return result;
    }

    private string GetOutputDirectory(FormatExport format)
    {
        return format switch
        {
            FormatExport.PDF => _appSettings.RepertoireStockagePDF,
            FormatExport.HTML => Path.Combine(_appSettings.RepertoireStockageImages, "exports"),
            _ => Path.Combine(Environment.CurrentDirectory, "exports")
        };
    }
}

// Stratégies d'export spécialisées

public class HtmlExportStrategy : IExportStrategy
{
    private readonly IHtmlTemplateService _htmlTemplateService;

    public HtmlExportStrategy(IHtmlTemplateService htmlTemplateService)
    {
        _htmlTemplateService = htmlTemplateService;
    }

    public async Task<ExportResult> ExportAsync(string content, ExportOptions options)
    {
        // Convertir le Markdown en HTML avec Markdig
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var htmlContent = Markdown.ToHtml(content, pipeline);
        
        // Créer un document HTML complet avec CSS
        var html = $@"
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
        
        var result = new ExportResult
        {
            Content = html,
            MimeType = GetMimeType(),
            FileSize = System.Text.Encoding.UTF8.GetByteCount(html)
        };

        if (options.SaveToFile && !string.IsNullOrEmpty(options.OutputDirectory))
        {
            var fileName = $"{options.FileName}{GetFileExtension()}";
            var filePath = Path.Combine(options.OutputDirectory, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, html);
            
            result.FilePath = filePath;
        }

        return result;
    }

    public string GetMimeType() => "text/html";
    public string GetFileExtension() => ".html";
}

public class MarkdownExportStrategy : IExportStrategy
{
    public async Task<ExportResult> ExportAsync(string content, ExportOptions options)
    {
        // Le contenu est déjà en Markdown
        await Task.CompletedTask;
        
        var result = new ExportResult
        {
            Content = content,
            MimeType = GetMimeType(),
            FileSize = System.Text.Encoding.UTF8.GetByteCount(content)
        };

        if (options.SaveToFile && !string.IsNullOrEmpty(options.OutputDirectory))
        {
            var fileName = $"{options.FileName}{GetFileExtension()}";
            var filePath = Path.Combine(options.OutputDirectory, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, content);
            
            result.FilePath = filePath;
        }

        return result;
    }

    public string GetMimeType() => "text/markdown";
    public string GetFileExtension() => ".md";
}

public class PdfExportStrategy : IExportStrategy
{
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IHtmlTemplateService _htmlTemplateService;
    private readonly AppSettings _appSettings;

    public PdfExportStrategy(IPdfGenerationService pdfGenerationService, 
        IHtmlTemplateService htmlTemplateService, AppSettings appSettings)
    {
        _pdfGenerationService = pdfGenerationService;
        _htmlTemplateService = htmlTemplateService;
        _appSettings = appSettings;
    }

    public async Task<ExportResult> ExportAsync(string content, ExportOptions options)
    {
        // Convertir le Markdown en HTML avec Markdig
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var htmlContent = Markdown.ToHtml(content, pipeline);
        
        // Créer un document HTML complet avec CSS
        var html = $@"
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
        
        // Générer le PDF
        var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(html);
        
        var result = new ExportResult
        {
            Content = Convert.ToBase64String(pdfBytes), // Encoder en Base64 pour le transport
            MimeType = GetMimeType(),
            FileSize = pdfBytes.Length
        };

        if (options.SaveToFile)
        {
            var fileName = $"{options.FileName}{GetFileExtension()}";
            var outputDir = !string.IsNullOrEmpty(options.OutputDirectory) 
                ? options.OutputDirectory 
                : _appSettings.RepertoireStockagePDF;
            var filePath = Path.Combine(outputDir, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            
            result.FilePath = filePath;
        }

        return result;
    }

    public string GetMimeType() => "application/pdf";
    public string GetFileExtension() => ".pdf";
}

public class WordExportStrategy : IExportStrategy
{
    public async Task<ExportResult> ExportAsync(string content, ExportOptions options)
    {
        // TODO: Implémentation Word réelle avec une librairie comme DocumentFormat.OpenXml
        await Task.Delay(10);
        
        var simulatedContent = $"[WORD] Document simulé :\n{content}";
        
        var result = new ExportResult
        {
            Content = simulatedContent,
            MimeType = GetMimeType(),
            FileSize = System.Text.Encoding.UTF8.GetByteCount(simulatedContent)
        };

        if (options.SaveToFile && !string.IsNullOrEmpty(options.OutputDirectory))
        {
            var fileName = $"{options.FileName}{GetFileExtension()}";
            var filePath = Path.Combine(options.OutputDirectory, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, simulatedContent);
            
            result.FilePath = filePath;
        }

        return result;
    }

    public string GetMimeType() => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public string GetFileExtension() => ".docx";
}