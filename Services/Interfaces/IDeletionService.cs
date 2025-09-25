using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

/// <summary>
/// Service centralisé pour orchestrer les suppressions complexes avec validation,
/// gestion des fichiers physiques et logging d'audit
/// </summary>
public interface IDeletionService
{
    /// <summary>
    /// Supprime un chantier avec gestion complète des dépendances
    /// </summary>
    /// <param name="chantierId">Identifiant du chantier à supprimer</param>
    /// <param name="options">Options de suppression</param>
    /// <returns>Résultat de la suppression avec détails</returns>
    Task<DeletionResult> DeleteChantierAsync(int chantierId, DeletionOptions? options = null);

    /// <summary>
    /// Supprime un document généré avec tous ses fichiers associés
    /// </summary>
    /// <param name="documentId">Identifiant du document à supprimer</param>
    /// <param name="options">Options de suppression</param>
    /// <returns>Résultat de la suppression avec détails</returns>
    Task<DeletionResult> DeleteDocumentAsync(int documentId, DeletionOptions? options = null);

    /// <summary>
    /// Supprime une fiche technique avec ses PDFs associés
    /// </summary>
    /// <param name="ficheTechniqueId">Identifiant de la fiche technique</param>
    /// <param name="options">Options de suppression</param>
    /// <returns>Résultat de la suppression avec détails</returns>
    Task<DeletionResult> DeleteFicheTechniqueAsync(int ficheTechniqueId, DeletionOptions? options = null);

    /// <summary>
    /// Vérifie quels éléments seraient affectés par la suppression d'un chantier
    /// </summary>
    /// <param name="chantierId">Identifiant du chantier</param>
    /// <returns>Impact prévu de la suppression</returns>
    Task<DeletionImpact> GetChantierDeletionImpactAsync(int chantierId);

    /// <summary>
    /// Vérifie quels éléments seraient affectés par la suppression d'un document
    /// </summary>
    /// <param name="documentId">Identifiant du document</param>
    /// <returns>Impact prévu de la suppression</returns>
    Task<DeletionImpact> GetDocumentDeletionImpactAsync(int documentId);

    /// <summary>
    /// Vérifie quels éléments seraient affectés par la suppression d'une fiche technique
    /// </summary>
    /// <param name="ficheTechniqueId">Identifiant de la fiche technique</param>
    /// <returns>Impact prévu de la suppression</returns>
    Task<DeletionImpact> GetFicheTechniqueDeletionImpactAsync(int ficheTechniqueId);

    /// <summary>
    /// Valide si un chantier peut être supprimé selon les règles métier
    /// </summary>
    /// <param name="chantierId">Identifiant du chantier</param>
    /// <returns>Résultat de validation avec messages d'erreur éventuels</returns>
    Task<ValidationResult> ValidateChantierDeletionAsync(int chantierId);

    /// <summary>
    /// Valide si un document peut être supprimé selon les règles métier
    /// </summary>
    /// <param name="documentId">Identifiant du document</param>
    /// <returns>Résultat de validation avec messages d'erreur éventuels</returns>
    Task<ValidationResult> ValidateDocumentDeletionAsync(int documentId);

    /// <summary>
    /// Détecte les références PDF orphelines (en base de données mais fichiers absents du système)
    /// </summary>
    /// <param name="options">Options pour la détection</param>
    /// <returns>Rapport détaillé des références orphelines trouvées</returns>
    Task<OrphanFilesReport> DetectOrphanReferencesAsync(OrphanDetectionOptions? options = null);

    /// <summary>
    /// Nettoie les références orphelines détectées
    /// </summary>
    /// <param name="report">Rapport des orphelins à nettoyer</param>
    /// <param name="options">Options de suppression</param>
    /// <returns>Résultat du nettoyage avec détails</returns>
    Task<DeletionResult> CleanupOrphanReferencesAsync(OrphanFilesReport report, DeletionOptions? options = null);

    /// <summary>
    /// Génère un rapport complet sur l'intégrité des fichiers
    /// </summary>
    /// <returns>Rapport de maintenance complet</returns>
    Task<FileIntegrityReport> GenerateFileIntegrityReportAsync();
}

/// <summary>
/// Options pour personnaliser le comportement de suppression
/// </summary>
public class DeletionOptions
{
    /// <summary>
    /// Force la suppression même si des dépendances existent
    /// </summary>
    public bool ForceDelete { get; set; } = false;

    /// <summary>
    /// Supprime les fichiers physiques (défaut: true)
    /// </summary>
    public bool DeletePhysicalFiles { get; set; } = true;

    /// <summary>
    /// Active les logs détaillés (défaut: true)
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Crée une sauvegarde avant suppression (défaut: false)
    /// </summary>
    public bool CreateBackup { get; set; } = false;

    /// <summary>
    /// Utilisateur qui initie la suppression (pour audit)
    /// </summary>
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Raison de la suppression (pour audit)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Résultat d'une opération de suppression
/// </summary>
public class DeletionResult
{
    /// <summary>
    /// Indique si la suppression a réussi
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messages d'information ou d'erreur
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Nombre de fichiers supprimés
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// Nombre d'enregistrements supprimés en base
    /// </summary>
    public int RecordsDeleted { get; set; }

    /// <summary>
    /// Durée de l'opération
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Exception capturée en cas d'erreur
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Identifiant de l'opération pour audit
    /// </summary>
    public Guid OperationId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Impact prévu d'une suppression (pour prévisualisation)
/// </summary>
public class DeletionImpact
{
    /// <summary>
    /// Nombre de documents qui seraient supprimés
    /// </summary>
    public int DocumentsCount { get; set; }

    /// <summary>
    /// Nombre de fiches techniques qui seraient supprimées
    /// </summary>
    public int FichesTechniquesCount { get; set; }

    /// <summary>
    /// Nombre de fichiers PDF qui seraient supprimés
    /// </summary>
    public int PdfFilesCount { get; set; }

    /// <summary>
    /// Nombre de sections libres qui seraient supprimées
    /// </summary>
    public int SectionsLibresCount { get; set; }

    /// <summary>
    /// Taille totale des fichiers qui seraient supprimés (en octets)
    /// </summary>
    public long TotalFileSize { get; set; }

    /// <summary>
    /// Liste des noms des éléments principaux affectés
    /// </summary>
    public List<string> AffectedItems { get; set; } = new();

    /// <summary>
    /// Avertissements spéciaux (ex: documents finalisés)
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Résultat de validation des règles métier
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indique si la suppression est autorisée
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Messages d'erreur de validation
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Avertissements non bloquants
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Options pour la détection des références orphelines
/// </summary>
public class OrphanDetectionOptions
{
    /// <summary>
    /// Vérifier les ImportPDF orphelins (défaut: true)
    /// </summary>
    public bool CheckImportPdfs { get; set; } = true;

    /// <summary>
    /// Vérifier les documents générés orphelins (défaut: true)
    /// </summary>
    public bool CheckDocumentsGeneres { get; set; } = true;

    /// <summary>
    /// Vérifier les images des méthodes orphelines (défaut: true)
    /// </summary>
    public bool CheckMethodeImages { get; set; } = true;

    /// <summary>
    /// Inclure les fichiers récemment créés (défaut: false)
    /// </summary>
    public bool IncludeRecentFiles { get; set; } = false;

    /// <summary>
    /// Délai pour considérer un fichier comme récent (défaut: 1 heure)
    /// </summary>
    public TimeSpan RecentFileThreshold { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Rapport détaillé des références orphelines détectées
/// </summary>
public class OrphanFilesReport
{
    /// <summary>
    /// Identifiant unique du rapport
    /// </summary>
    public Guid ReportId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date et heure de génération du rapport
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// ImportPDF orphelins (références en base, fichiers absents)
    /// </summary>
    public List<OrphanPdfInfo> OrphanImportPdfs { get; set; } = new();

    /// <summary>
    /// Documents générés orphelins
    /// </summary>
    public List<OrphanDocumentInfo> OrphanDocuments { get; set; } = new();

    /// <summary>
    /// Images de méthodes orphelines
    /// </summary>
    public List<OrphanImageInfo> OrphanMethodeImages { get; set; } = new();

    /// <summary>
    /// Nombre total de références orphelines
    /// </summary>
    public int TotalOrphanReferences => OrphanImportPdfs.Count + OrphanDocuments.Count + OrphanMethodeImages.Count;

    /// <summary>
    /// Taille estimée des données à nettoyer en base (octets)
    /// </summary>
    public long EstimatedDatabaseCleanupSize { get; set; }

    /// <summary>
    /// Durée du scan
    /// </summary>
    public TimeSpan ScanDuration { get; set; }
}

/// <summary>
/// Information sur un PDF orphelin
/// </summary>
public class OrphanPdfInfo
{
    public int ImportPdfId { get; set; }
    public string CheminFichier { get; set; } = string.Empty;
    public string NomFichierOriginal { get; set; } = string.Empty;
    public string FicheTechniqueName { get; set; } = string.Empty;
    public DateTime DateImport { get; set; }
    public long TailleFichier { get; set; }
}

/// <summary>
/// Information sur un document généré orphelin
/// </summary>
public class OrphanDocumentInfo
{
    public int DocumentId { get; set; }
    public string CheminFichier { get; set; } = string.Empty;
    public string NomFichier { get; set; } = string.Empty;
    public string ChantierNom { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
}

/// <summary>
/// Information sur une image de méthode orpheline
/// </summary>
public class OrphanImageInfo
{
    public int MethodeId { get; set; }
    public string CheminImage { get; set; } = string.Empty;
    public string NomImage { get; set; } = string.Empty;
    public string MethodeTitre { get; set; } = string.Empty;
}

/// <summary>
/// Rapport complet sur l'intégrité des fichiers du système
/// </summary>
public class FileIntegrityReport
{
    /// <summary>
    /// Rapport des orphelins détectés
    /// </summary>
    public OrphanFilesReport OrphansReport { get; set; } = new();

    /// <summary>
    /// Nombre total de fichiers PDF valides
    /// </summary>
    public int ValidPdfFilesCount { get; set; }

    /// <summary>
    /// Nombre total de documents valides
    /// </summary>
    public int ValidDocumentsCount { get; set; }

    /// <summary>
    /// Taille totale des fichiers valides (octets)
    /// </summary>
    public long TotalValidFilesSize { get; set; }

    /// <summary>
    /// Recommandations de maintenance
    /// </summary>
    public List<string> MaintenanceRecommendations { get; set; } = new();

    /// <summary>
    /// Score de santé du système (0-100)
    /// </summary>
    public int HealthScore { get; set; }
}