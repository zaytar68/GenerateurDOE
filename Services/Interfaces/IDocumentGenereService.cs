using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentGenereService
{
    Task<string> ExportDocumentAsync(int chantierId, TypeDocumentGenere typeDocument, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDOEAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDossierTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateMemoireTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<DocumentGenere> SaveDocumentGenereAsync(DocumentGenere documentGenere);
    Task<IEnumerable<DocumentGenere>> GetDocumentsGeneresByChantierId(int chantierId);
    Task<bool> DeleteDocumentGenereAsync(int documentGenereId);
}