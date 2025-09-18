using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services;

public class DocumentDownloadService : IDocumentDownloadService
{
    private readonly IDocumentGenereService _documentGenereService;
    private readonly ILogger<DocumentDownloadService> _logger;

    public DocumentDownloadService(
        IDocumentGenereService documentGenereService,
        ILogger<DocumentDownloadService> logger)
    {
        _documentGenereService = documentGenereService;
        _logger = logger;
    }

    public async Task<DocumentDownloadResult> PrepareDocumentForDownloadAsync(int documentId)
    {
        try
        {
            _logger.LogInformation("Préparation du téléchargement pour le document {DocumentId}", documentId);

            // Récupérer les informations du document
            var document = await _documentGenereService.GetByIdAsync(documentId);
            if (document == null)
            {
                return new DocumentDownloadResult
                {
                    Success = false,
                    ErrorMessage = "Document introuvable"
                };
            }

            byte[] fileBytes;

            // Génération selon le format demandé
            if (document.FormatExport == FormatExport.PDF)
            {
                // Utilisation de la génération PDF complète avec PuppeteerSharp + PDFSharp
                fileBytes = await _documentGenereService.GenerateCompletePdfAsync(document.Id);
                _logger.LogInformation("PDF généré avec succès pour le document {DocumentId}", documentId);
            }
            else
            {
                // Pour les autres formats (HTML, Markdown), utiliser l'export standard
                var cheminFichier = await _documentGenereService.ExportDocumentAsync(document.Id, document.FormatExport);

                if (File.Exists(cheminFichier))
                {
                    fileBytes = await File.ReadAllBytesAsync(cheminFichier);
                    _logger.LogInformation("Document {Format} généré pour le document {DocumentId}", document.FormatExport, documentId);
                }
                else
                {
                    return new DocumentDownloadResult
                    {
                        Success = false,
                        ErrorMessage = "Le fichier généré n'a pas été trouvé"
                    };
                }
            }

            // Construction du nom de fichier avec timestamp
            var fileName = $"{document.NomFichier}_{DateTime.Now:yyyyMMdd_HHmm}.{GetFileExtension(document.FormatExport)}";
            var mimeType = GetMimeType(document.FormatExport);
            var fileExtension = GetFileExtension(document.FormatExport);

            return new DocumentDownloadResult
            {
                FileBytes = fileBytes,
                FileName = fileName,
                MimeType = mimeType,
                FileExtension = fileExtension,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la préparation du téléchargement pour le document {DocumentId}", documentId);

            // Masquer les erreurs de concurrence DbContext (temporaire)
            var errorMessage = ex.Message.Contains("A second operation was started on this context") ||
                               ex.Message.Contains("context instance")
                ? "Erreur temporaire, veuillez réessayer"
                : $"Erreur lors de la génération: {ex.Message}";

            return new DocumentDownloadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    private static string GetFileExtension(FormatExport format)
    {
        return format switch
        {
            FormatExport.PDF => "pdf",
            FormatExport.HTML => "html",
            FormatExport.Markdown => "md",
            FormatExport.Word => "docx",
            _ => "pdf"
        };
    }

    private static string GetMimeType(FormatExport format)
    {
        return format switch
        {
            FormatExport.PDF => "application/pdf",
            FormatExport.HTML => "text/html",
            FormatExport.Markdown => "text/markdown",
            FormatExport.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/pdf"
        };
    }
}