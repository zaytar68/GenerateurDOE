using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentExportService
{
    Task<string> ExportDocumentAsync(int chantierId, TypeDocumentExport typeDocument, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDOEAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDossierTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateMemoireTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<DocumentExport> SaveDocumentExportAsync(DocumentExport documentExport);
    Task<IEnumerable<DocumentExport>> GetDocumentExportsByChantierId(int chantierId);
    Task<bool> DeleteDocumentExportAsync(int documentExportId);
}