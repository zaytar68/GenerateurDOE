using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service centralisé pour orchestrer les suppressions complexes avec validation,
/// gestion des fichiers physiques et logging d'audit
/// </summary>
public class DeletionService : IDeletionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILoggingService _logger;
    private readonly IFicheTechniqueService _ficheService;

    public DeletionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILoggingService logger,
        IFicheTechniqueService ficheService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _ficheService = ficheService;
    }

    public async Task<DeletionResult> DeleteChantierAsync(int chantierId, DeletionOptions? options = null)
    {
        options ??= new DeletionOptions();
        var result = new DeletionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (options.EnableAuditLogging)
            {
                _logger.LogInformation($"[DELETION] Début suppression chantier ID={chantierId} par {options.InitiatedBy ?? "Système"}. Raison: {options.Reason ?? "Non spécifiée"}");
            }

            // 1. Validation métier
            var validation = await ValidateChantierDeletionAsync(chantierId);
            if (!validation.IsValid && !options.ForceDelete)
            {
                result.Success = false;
                result.Messages.AddRange(validation.Errors);
                return result;
            }

            // 2. Évaluation de l'impact
            var impact = await GetChantierDeletionImpactAsync(chantierId);

            // 3. Suppression en cascade des dépendances
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var chantier = await context.Chantiers
                    .Include(c => c.DocumentsGeneres)
                        .ThenInclude(d => d.SectionsConteneurs)
                            .ThenInclude(sc => sc.Items)
                    .Include(c => c.DocumentsGeneres)
                        .ThenInclude(d => d.FTConteneur)
                            .ThenInclude(ft => ft.Elements)
                    .Include(c => c.FichesTechniques)
                        .ThenInclude(f => f.ImportsPDF)
                    .FirstOrDefaultAsync(c => c.Id == chantierId);

                if (chantier == null)
                {
                    result.Success = false;
                    result.Messages.Add($"Chantier avec ID {chantierId} introuvable");
                    return result;
                }

                // Suppression des documents générés et leurs fichiers
                foreach (var document in chantier.DocumentsGeneres)
                {
                    var docResult = await DeleteDocumentInternalAsync(document, options, context);
                    result.FilesDeleted += docResult.FilesDeleted;
                    result.RecordsDeleted += docResult.RecordsDeleted;
                    result.Messages.AddRange(docResult.Messages);
                }

                // Suppression des fiches techniques et leurs PDFs
                foreach (var fiche in chantier.FichesTechniques)
                {
                    var ficheResult = await DeleteFicheTechniqueInternalAsync(fiche, options, context);
                    result.FilesDeleted += ficheResult.FilesDeleted;
                    result.RecordsDeleted += ficheResult.RecordsDeleted;
                    result.Messages.AddRange(ficheResult.Messages);
                }

                // Suppression du chantier
                context.Chantiers.Remove(chantier);
                await context.SaveChangesAsync();
                result.RecordsDeleted++;

                await transaction.CommitAsync();
                result.Success = true;
                result.Messages.Add($"Chantier '{chantier.NomProjet}' supprimé avec succès");

                if (options.EnableAuditLogging)
                {
                    _logger.LogInformation($"[DELETION] Chantier ID={chantierId} supprimé avec succès. Documents: {impact.DocumentsCount}, Fiches: {impact.FichesTechniquesCount}, Fichiers: {result.FilesDeleted}");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Messages.Add($"Erreur lors de la suppression: {ex.Message}");

            if (options.EnableAuditLogging)
            {
                _logger.LogError(ex, $"[DELETION] Échec suppression chantier ID={chantierId}: {ex.Message}");
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<DeletionResult> DeleteDocumentAsync(int documentId, DeletionOptions? options = null)
    {
        options ??= new DeletionOptions();
        var result = new DeletionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (options.EnableAuditLogging)
            {
                _logger.LogInformation($"[DELETION] Début suppression document ID={documentId} par {options.InitiatedBy ?? "Système"}");
            }

            var validation = await ValidateDocumentDeletionAsync(documentId);
            if (!validation.IsValid && !options.ForceDelete)
            {
                result.Success = false;
                result.Messages.AddRange(validation.Errors);
                return result;
            }

            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var document = await context.DocumentsGeneres
                .Include(d => d.SectionsConteneurs)
                    .ThenInclude(sc => sc.Items)
                .Include(d => d.FTConteneur)
                    .ThenInclude(ft => ft.Elements)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                result.Success = false;
                result.Messages.Add($"Document avec ID {documentId} introuvable");
                return result;
            }

            result = await DeleteDocumentInternalAsync(document, options, context);

            if (options.EnableAuditLogging && result.Success)
            {
                _logger.LogInformation($"[DELETION] Document ID={documentId} '{document.NomFichier}' supprimé avec succès");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Messages.Add($"Erreur lors de la suppression: {ex.Message}");

            if (options.EnableAuditLogging)
            {
                _logger.LogError(ex, $"[DELETION] Échec suppression document ID={documentId}: {ex.Message}");
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<DeletionResult> DeleteFicheTechniqueAsync(int ficheTechniqueId, DeletionOptions? options = null)
    {
        options ??= new DeletionOptions();
        var result = new DeletionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (options.EnableAuditLogging)
            {
                _logger.LogInformation($"[DELETION] Début suppression fiche technique ID={ficheTechniqueId} par {options.InitiatedBy ?? "Système"}");
            }

            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var fiche = await context.FichesTechniques
                .Include(f => f.ImportsPDF)
                .FirstOrDefaultAsync(f => f.Id == ficheTechniqueId);

            if (fiche == null)
            {
                result.Success = false;
                result.Messages.Add($"Fiche technique avec ID {ficheTechniqueId} introuvable");
                return result;
            }

            result = await DeleteFicheTechniqueInternalAsync(fiche, options, context);

            if (options.EnableAuditLogging && result.Success)
            {
                _logger.LogInformation($"[DELETION] Fiche technique ID={ficheTechniqueId} '{fiche.NomProduit}' supprimée avec succès");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Messages.Add($"Erreur lors de la suppression: {ex.Message}");

            if (options.EnableAuditLogging)
            {
                _logger.LogError(ex, $"[DELETION] Échec suppression fiche technique ID={ficheTechniqueId}: {ex.Message}");
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<DeletionImpact> GetChantierDeletionImpactAsync(int chantierId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var impact = new DeletionImpact();

        var chantier = await context.Chantiers
            .Include(c => c.DocumentsGeneres)
            .Include(c => c.FichesTechniques)
                .ThenInclude(f => f.ImportsPDF)
            .FirstOrDefaultAsync(c => c.Id == chantierId);

        if (chantier == null) return impact;

        impact.DocumentsCount = chantier.DocumentsGeneres.Count;
        impact.FichesTechniquesCount = chantier.FichesTechniques.Count;
        impact.PdfFilesCount = chantier.FichesTechniques.SelectMany(f => f.ImportsPDF).Count();

        // Calcul des sections libres
        var sectionsCount = await context.SectionsConteneurs
            .Where(sc => chantier.DocumentsGeneres.Select(d => d.Id).Contains(sc.DocumentGenereId))
            .SelectMany(sc => sc.Items)
            .CountAsync();
        impact.SectionsLibresCount = sectionsCount;

        // Calcul de la taille des fichiers
        foreach (var fiche in chantier.FichesTechniques)
        {
            impact.TotalFileSize += fiche.ImportsPDF.Sum(pdf => pdf.TailleFichier);
        }

        // Fichiers de documents générés
        foreach (var doc in chantier.DocumentsGeneres.Where(d => !string.IsNullOrEmpty(d.CheminFichier)))
        {
            if (File.Exists(doc.CheminFichier))
            {
                var fileInfo = new FileInfo(doc.CheminFichier);
                impact.TotalFileSize += fileInfo.Length;
                impact.PdfFilesCount++;
            }
        }

        // Liste des éléments affectés
        impact.AffectedItems.Add($"Chantier: {chantier.NomProjet}");
        impact.AffectedItems.AddRange(chantier.DocumentsGeneres.Select(d => $"Document: {d.NomFichier}"));
        impact.AffectedItems.AddRange(chantier.FichesTechniques.Select(f => $"Fiche: {f.NomProduit} ({f.NomFabricant})"));

        // Avertissements
        var documentsFinalized = chantier.DocumentsGeneres.Count(d => !d.EnCours);
        if (documentsFinalized > 0)
        {
            impact.Warnings.Add($"{documentsFinalized} document(s) finalisé(s) seront supprimés définitivement");
        }

        return impact;
    }

    public async Task<DeletionImpact> GetDocumentDeletionImpactAsync(int documentId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var impact = new DeletionImpact();

        var document = await context.DocumentsGeneres
            .Include(d => d.SectionsConteneurs)
                .ThenInclude(sc => sc.Items)
            .Include(d => d.FTConteneur)
                .ThenInclude(ft => ft.Elements)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null) return impact;

        impact.DocumentsCount = 1;
        impact.SectionsLibresCount = document.SectionsConteneurs.SelectMany(sc => sc.Items).Count();

        if (!string.IsNullOrEmpty(document.CheminFichier) && File.Exists(document.CheminFichier))
        {
            var fileInfo = new FileInfo(document.CheminFichier);
            impact.TotalFileSize = fileInfo.Length;
            impact.PdfFilesCount = 1;
        }

        impact.AffectedItems.Add($"Document: {document.NomFichier}");

        if (!document.EnCours)
        {
            impact.Warnings.Add("Ce document est finalisé et sera supprimé définitivement");
        }

        return impact;
    }

    public async Task<DeletionImpact> GetFicheTechniqueDeletionImpactAsync(int ficheTechniqueId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var impact = new DeletionImpact();

        var fiche = await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .FirstOrDefaultAsync(f => f.Id == ficheTechniqueId);

        if (fiche == null) return impact;

        impact.FichesTechniquesCount = 1;
        impact.PdfFilesCount = fiche.ImportsPDF.Count;
        impact.TotalFileSize = fiche.ImportsPDF.Sum(pdf => pdf.TailleFichier);

        impact.AffectedItems.Add($"Fiche: {fiche.NomProduit} ({fiche.NomFabricant})");
        impact.AffectedItems.AddRange(fiche.ImportsPDF.Select(pdf => $"PDF: {pdf.NomFichierOriginal}"));

        return impact;
    }

    public async Task<ValidationResult> ValidateChantierDeletionAsync(int chantierId)
    {
        var result = new ValidationResult();

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var chantier = await context.Chantiers
            .Include(c => c.DocumentsGeneres)
            .FirstOrDefaultAsync(c => c.Id == chantierId);

        if (chantier == null)
        {
            result.IsValid = false;
            result.Errors.Add("Chantier introuvable");
            return result;
        }

        // Règles métier
        var documentsEnCours = chantier.DocumentsGeneres.Count(d => d.EnCours);
        if (documentsEnCours > 0)
        {
            result.Warnings.Add($"{documentsEnCours} document(s) en cours seront supprimés");
        }

        var documentsFinalized = chantier.DocumentsGeneres.Count(d => !d.EnCours);
        if (documentsFinalized > 0)
        {
            result.Warnings.Add($"{documentsFinalized} document(s) finalisé(s) seront supprimés définitivement");
        }

        // Validation réussie (pas de règles bloquantes pour l'instant)
        result.IsValid = true;
        return result;
    }

    public async Task<ValidationResult> ValidateDocumentDeletionAsync(int documentId)
    {
        var result = new ValidationResult();

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var document = await context.DocumentsGeneres.FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            result.IsValid = false;
            result.Errors.Add("Document introuvable");
            return result;
        }

        if (!document.EnCours)
        {
            result.Warnings.Add("Ce document est finalisé et sera supprimé définitivement");
        }

        result.IsValid = true;
        return result;
    }

    #region Méthodes internes

    private async Task<DeletionResult> DeleteDocumentInternalAsync(DocumentGenere document, DeletionOptions options, ApplicationDbContext context)
    {
        var result = new DeletionResult { Success = true };

        // Suppression du fichier PDF généré
        if (options.DeletePhysicalFiles && !string.IsNullOrEmpty(document.CheminFichier) && File.Exists(document.CheminFichier))
        {
            try
            {
                File.Delete(document.CheminFichier);
                result.FilesDeleted++;
                result.Messages.Add($"Fichier PDF supprimé: {document.CheminFichier}");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"Impossible de supprimer le fichier {document.CheminFichier}: {ex.Message}");
            }
        }

        // EF Core gère la cascade delete pour les relations
        context.DocumentsGeneres.Remove(document);
        await context.SaveChangesAsync();
        result.RecordsDeleted++;

        return result;
    }

    private async Task<DeletionResult> DeleteFicheTechniqueInternalAsync(FicheTechnique fiche, DeletionOptions options, ApplicationDbContext context)
    {
        var result = new DeletionResult { Success = true };

        // Suppression des fichiers PDF
        if (options.DeletePhysicalFiles)
        {
            foreach (var pdf in fiche.ImportsPDF)
            {
                if (File.Exists(pdf.CheminFichier))
                {
                    try
                    {
                        File.Delete(pdf.CheminFichier);
                        result.FilesDeleted++;
                        result.Messages.Add($"Fichier PDF supprimé: {pdf.NomFichierOriginal}");
                    }
                    catch (Exception ex)
                    {
                        result.Messages.Add($"Impossible de supprimer le fichier {pdf.NomFichierOriginal}: {ex.Message}");
                    }
                }
            }
        }

        // EF Core gère la cascade delete pour les ImportsPDF
        context.FichesTechniques.Remove(fiche);
        await context.SaveChangesAsync();
        result.RecordsDeleted++;

        return result;
    }

    #endregion

    #region Orphan References Management

    public async Task<OrphanFilesReport> DetectOrphanReferencesAsync(OrphanDetectionOptions? options = null)
    {
        options ??= new OrphanDetectionOptions();
        var report = new OrphanFilesReport();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("[ORPHAN-DETECTION] Début du scan des références orphelines");

            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            // 1. Détecter les ImportPDF orphelins
            if (options.CheckImportPdfs)
            {
                await DetectOrphanImportPdfsAsync(context, report, options).ConfigureAwait(false);
            }

            // 2. Détecter les documents générés orphelins
            if (options.CheckDocumentsGeneres)
            {
                await DetectOrphanDocumentsAsync(context, report, options).ConfigureAwait(false);
            }

            // 3. Détecter les images de méthodes orphelines
            if (options.CheckMethodeImages)
            {
                await DetectOrphanMethodeImagesAsync(context, report, options).ConfigureAwait(false);
            }

            report.ScanDuration = stopwatch.Elapsed;
            _logger.LogInformation($"[ORPHAN-DETECTION] Scan terminé en {report.ScanDuration.TotalSeconds:F2}s. {report.TotalOrphanReferences} références orphelines détectées");

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ORPHAN-DETECTION] Erreur lors du scan: {ex.Message}");
            throw;
        }
    }

    public async Task<DeletionResult> CleanupOrphanReferencesAsync(OrphanFilesReport report, DeletionOptions? options = null)
    {
        options ??= new DeletionOptions();
        var result = new DeletionResult();
        var stopwatch = Stopwatch.StartNew();

        if (report.TotalOrphanReferences == 0)
        {
            result.Success = true;
            result.Messages.Add("Aucune référence orpheline à nettoyer");
            return result;
        }

        try
        {
            if (options.EnableAuditLogging)
            {
                _logger.LogInformation($"[ORPHAN-CLEANUP] Début du nettoyage de {report.TotalOrphanReferences} références orphelines par {options.InitiatedBy ?? "Système"}");
            }

            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                // 1. Nettoyer les ImportPDF orphelins
                await CleanupOrphanImportPdfsAsync(context, report.OrphanImportPdfs, result).ConfigureAwait(false);

                // 2. Nettoyer les documents orphelins
                await CleanupOrphanDocumentsAsync(context, report.OrphanDocuments, result).ConfigureAwait(false);

                // 3. Nettoyer les images de méthodes orphelines
                await CleanupOrphanMethodeImagesAsync(context, report.OrphanMethodeImages, result).ConfigureAwait(false);

                await context.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                result.Success = true;
                result.Duration = stopwatch.Elapsed;
                result.Messages.Add($"Nettoyage terminé avec succès en {result.Duration.TotalSeconds:F2}s");

                if (options.EnableAuditLogging)
                {
                    _logger.LogInformation($"[ORPHAN-CLEANUP] {result.RecordsDeleted} références nettoyées avec succès");
                }
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Messages.Add($"Erreur lors du nettoyage: {ex.Message}");
            _logger.LogError($"[ORPHAN-CLEANUP] Erreur: {ex.Message}");
        }

        return result;
    }

    public async Task<FileIntegrityReport> GenerateFileIntegrityReportAsync()
    {
        var report = new FileIntegrityReport();

        try
        {
            _logger.LogInformation("[FILE-INTEGRITY] Génération du rapport d'intégrité des fichiers");

            // 1. Détecter les orphelins
            report.OrphansReport = await DetectOrphanReferencesAsync().ConfigureAwait(false);

            // 2. Compter les fichiers valides
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            // Compter les PDFs valides
            var validImportPdfs = await context.ImportsPDF
                .Where(p => !string.IsNullOrEmpty(p.CheminFichier))
                .ToListAsync().ConfigureAwait(false);

            foreach (var pdf in validImportPdfs)
            {
                if (File.Exists(pdf.CheminFichier))
                {
                    report.ValidPdfFilesCount++;
                    var fileInfo = new FileInfo(pdf.CheminFichier);
                    report.TotalValidFilesSize += fileInfo.Length;
                }
            }

            // Compter les documents valides
            var validDocuments = await context.DocumentsGeneres
                .Where(d => !string.IsNullOrEmpty(d.CheminFichier))
                .ToListAsync().ConfigureAwait(false);

            foreach (var doc in validDocuments)
            {
                if (File.Exists(doc.CheminFichier))
                {
                    report.ValidDocumentsCount++;
                    var fileInfo = new FileInfo(doc.CheminFichier);
                    report.TotalValidFilesSize += fileInfo.Length;
                }
            }

            // 3. Calculer le score de santé
            var totalFiles = report.ValidPdfFilesCount + report.ValidDocumentsCount;
            var totalOrphans = report.OrphansReport.TotalOrphanReferences;

            if (totalFiles + totalOrphans > 0)
            {
                report.HealthScore = (int)((double)totalFiles / (totalFiles + totalOrphans) * 100);
            }
            else
            {
                report.HealthScore = 100;
            }

            // 4. Générer les recommandations
            GenerateMaintenanceRecommendations(report);

            _logger.LogInformation($"[FILE-INTEGRITY] Rapport généré: {report.ValidPdfFilesCount + report.ValidDocumentsCount} fichiers valides, {totalOrphans} orphelins, score santé: {report.HealthScore}%");

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[FILE-INTEGRITY] Erreur lors de la génération du rapport: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Private Orphan Detection Methods

    private async Task DetectOrphanImportPdfsAsync(ApplicationDbContext context, OrphanFilesReport report, OrphanDetectionOptions options)
    {
        var importPdfs = await context.ImportsPDF
            .Include(p => p.FicheTechnique)
            .Where(p => !string.IsNullOrEmpty(p.CheminFichier))
            .ToListAsync().ConfigureAwait(false);

        foreach (var pdf in importPdfs)
        {
            if (!File.Exists(pdf.CheminFichier))
            {
                // Vérifier si c'est un fichier récent (optionnel)
                if (!options.IncludeRecentFiles &&
                    pdf.DateImport > DateTime.Now.Subtract(options.RecentFileThreshold))
                {
                    continue;
                }

                report.OrphanImportPdfs.Add(new OrphanPdfInfo
                {
                    ImportPdfId = pdf.Id,
                    CheminFichier = pdf.CheminFichier,
                    NomFichierOriginal = pdf.NomFichierOriginal,
                    FicheTechniqueName = pdf.FicheTechnique?.NomProduit ?? "Inconnu",
                    DateImport = pdf.DateImport,
                    TailleFichier = pdf.TailleFichier
                });

                report.EstimatedDatabaseCleanupSize += pdf.TailleFichier;
            }
        }
    }

    private async Task DetectOrphanDocumentsAsync(ApplicationDbContext context, OrphanFilesReport report, OrphanDetectionOptions options)
    {
        var documents = await context.DocumentsGeneres
            .Include(d => d.Chantier)
            .Where(d => !string.IsNullOrEmpty(d.CheminFichier))
            .ToListAsync().ConfigureAwait(false);

        foreach (var doc in documents)
        {
            if (!File.Exists(doc.CheminFichier))
            {
                if (!options.IncludeRecentFiles &&
                    doc.DateCreation > DateTime.Now.Subtract(options.RecentFileThreshold))
                {
                    continue;
                }

                report.OrphanDocuments.Add(new OrphanDocumentInfo
                {
                    DocumentId = doc.Id,
                    CheminFichier = doc.CheminFichier,
                    NomFichier = doc.NomFichier,
                    ChantierNom = doc.Chantier?.NomProjet ?? "Inconnu",
                    DateCreation = doc.DateCreation
                });
            }
        }
    }

    private async Task DetectOrphanMethodeImagesAsync(ApplicationDbContext context, OrphanFilesReport report, OrphanDetectionOptions options)
    {
        var imagesMethodes = await context.ImagesMethode
            .Include(im => im.Methode)
            .Where(im => !string.IsNullOrEmpty(im.CheminFichier))
            .ToListAsync().ConfigureAwait(false);

        foreach (var image in imagesMethodes)
        {
            if (!File.Exists(image.CheminFichier))
            {
                report.OrphanMethodeImages.Add(new OrphanImageInfo
                {
                    MethodeId = image.MethodeId,
                    CheminImage = image.CheminFichier,
                    NomImage = image.NomFichierOriginal ?? Path.GetFileName(image.CheminFichier),
                    MethodeTitre = image.Methode?.Titre ?? "Méthode inconnue"
                });
            }
        }
    }

    private async Task CleanupOrphanImportPdfsAsync(ApplicationDbContext context, List<OrphanPdfInfo> orphans, DeletionResult result)
    {
        if (orphans.Count == 0) return;

        var orphanIds = orphans.Select(o => o.ImportPdfId).ToList();
        var orphanPdfs = await context.ImportsPDF
            .Where(p => orphanIds.Contains(p.Id))
            .ToListAsync().ConfigureAwait(false);

        context.ImportsPDF.RemoveRange(orphanPdfs);
        result.RecordsDeleted += orphanPdfs.Count;
        result.Messages.Add($"Supprimé {orphanPdfs.Count} références ImportPDF orphelines");
    }

    private async Task CleanupOrphanDocumentsAsync(ApplicationDbContext context, List<OrphanDocumentInfo> orphans, DeletionResult result)
    {
        if (orphans.Count == 0) return;

        foreach (var orphan in orphans)
        {
            // Nettoyer seulement le chemin de fichier, garder le document
            var document = await context.DocumentsGeneres
                .FirstOrDefaultAsync(d => d.Id == orphan.DocumentId).ConfigureAwait(false);

            if (document != null)
            {
                document.CheminFichier = string.Empty;
                result.RecordsDeleted++;
            }
        }

        result.Messages.Add($"Nettoyé {orphans.Count} chemins de documents orphelins");
    }

    private async Task CleanupOrphanMethodeImagesAsync(ApplicationDbContext context, List<OrphanImageInfo> orphans, DeletionResult result)
    {
        if (orphans.Count == 0) return;

        var orphanImagePaths = orphans.Select(o => o.CheminImage).ToList();
        var orphanImages = await context.ImagesMethode
            .Where(im => orphanImagePaths.Contains(im.CheminFichier))
            .ToListAsync().ConfigureAwait(false);

        context.ImagesMethode.RemoveRange(orphanImages);
        result.RecordsDeleted += orphanImages.Count;
        result.Messages.Add($"Supprimé {orphanImages.Count} références d'images de méthodes orphelines");
    }

    private static void GenerateMaintenanceRecommendations(FileIntegrityReport report)
    {
        var recommendations = report.MaintenanceRecommendations;

        if (report.OrphansReport.TotalOrphanReferences > 0)
        {
            recommendations.Add($"🔧 Nettoyer {report.OrphansReport.TotalOrphanReferences} références orphelines détectées");
        }

        if (report.HealthScore < 90)
        {
            recommendations.Add("⚠️ Score de santé bas - effectuer une maintenance préventive");
        }

        if (report.OrphansReport.OrphanImportPdfs.Count > 10)
        {
            recommendations.Add("📁 Vérifier la configuration des répertoires PDF");
        }

        if (report.OrphansReport.OrphanDocuments.Count > 5)
        {
            recommendations.Add("📄 Vérifier les processus de génération de documents");
        }

        if (report.HealthScore >= 95 && report.OrphansReport.TotalOrphanReferences == 0)
        {
            recommendations.Add("✅ Système en excellent état - aucune action requise");
        }
    }

    #endregion
}