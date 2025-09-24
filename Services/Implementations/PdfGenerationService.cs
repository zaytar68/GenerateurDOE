using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GenerateurDOE.Services.Implementations
{
    /// <summary>
    /// Service de génération PDF avec architecture hybride PuppeteerSharp + PDFSharp
    /// Gère la conversion HTML→PDF, l'assembly de multiples PDFs et l'optimisation
    /// </summary>
    public class PdfGenerationService : IPdfGenerationService, IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILoggingService _loggingService;
        private readonly IPageGardeTemplateService _pageGardeTemplateService;
        private readonly IHtmlTemplateService _htmlTemplateService;
        private readonly IPdfProgressService _progressService;
        private readonly IPdfPageCountService _pdfPageCountService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IBrowser? _browser;
        private readonly SemaphoreSlim _browserSemaphore = new(1, 1);

        /// <summary>
        /// Initialise une nouvelle instance du service PdfGenerationService
        /// </summary>
        /// <param name="appSettings">Configuration de l'application</param>
        /// <param name="loggingService">Service de logging centralisé</param>
        /// <param name="pageGardeTemplateService">Service de templates de page de garde</param>
        /// <param name="htmlTemplateService">Service de templates HTML professionnels</param>
        /// <param name="progressService">Service de suivi de progression PDF en temps réel</param>
        /// <param name="webHostEnvironment">Environnement d'hébergement pour accès aux ressources</param>
        public PdfGenerationService(
            IConfigurationService configurationService,
            ILoggingService loggingService,
            IPageGardeTemplateService pageGardeTemplateService,
            IHtmlTemplateService htmlTemplateService,
            IPdfProgressService progressService,
            IPdfPageCountService pdfPageCountService,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            _configurationService = configurationService;
            _loggingService = loggingService;
            _pageGardeTemplateService = pageGardeTemplateService;
            _htmlTemplateService = htmlTemplateService;
            _progressService = progressService;
            _pdfPageCountService = pdfPageCountService;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Récupère ou crée une instance de navigateur Chromium pour la conversion HTML→PDF
        /// Utilise un singleton thread-safe avec SemaphoreSlim pour éviter les conflits
        /// </summary>
        /// <returns>Instance de navigateur PuppeteerSharp configurée</returns>
        private async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null)
            {
                await _browserSemaphore.WaitAsync();
                try
                {
                    if (_browser == null)
                    {
                        var launchOptions = new LaunchOptions
                        {
                            Headless = true,
                            Args = new[]
                            {
                                "--no-sandbox",
                                "--disable-setuid-sandbox",
                                "--disable-dev-shm-usage",
                                "--disable-gpu",
                                "--disable-web-security",
                                "--allow-running-insecure-content"
                            }
                        };

                        await new BrowserFetcher().DownloadAsync();
                        _browser = await Puppeteer.LaunchAsync(launchOptions);
                        
                        _loggingService.LogInformation("Browser Chromium initialisé pour PDF Generation");
                    }
                }
                finally
                {
                    _browserSemaphore.Release();
                }
            }

            return _browser;
        }

        /// <summary>
        /// Obtient l'URL de base dynamique du serveur en cours d'exécution
        /// Utilise le contexte HTTP pour détecter automatiquement le scheme, host et port
        /// </summary>
        /// <returns>URL de base complète (ex: http://localhost:5282)</returns>
        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                _loggingService.LogInformation($"URL de base détectée dynamiquement : {baseUrl}");
                return baseUrl;
            }

            // Fallback si pas de contexte HTTP disponible
            var fallbackUrl = "http://localhost:5282";
            _loggingService.LogWarning($"Aucun contexte HTTP disponible, utilisation du fallback : {fallbackUrl}");
            return fallbackUrl;
        }

        /// <summary>
        /// Génère un PDF complet en assemblant page de garde, table des matières, sections libres et fiches techniques
        /// Architecture hybride : HTML→PDF (PuppeteerSharp) + Assembly (PDFSharp) + Optimisation
        /// </summary>
        /// <param name="document">Document avec toutes ses sections chargées</param>
        /// <param name="options">Options de génération PDF personnalisées</param>
        /// <returns>PDF final optimisé avec métadonnées et signets</returns>
        /// <exception cref="Exception">Erreur durant la génération avec tracking dans progressService</exception>
        public async Task<byte[]> GenerateCompletePdfAsync(DocumentGenere document, PdfGenerationOptions? options = null)
        {
            _loggingService.LogInformation($"🔥 DÉBOGAGE: Génération PDF complète pour document {document.Id}");
            _loggingService.LogInformation($"🔥 DÉBOGAGE: IncludeTableMatieres = {document.IncludeTableMatieres}");

            // Récupérer les paramètres actuels de configuration
            var appSettings = await _configurationService.GetAppSettingsAsync();

            try
            {
                // Initialiser la progression
                _progressService.InitializeProgress(document.Id);

                var pdfParts = new List<byte[]>();
                options ??= new PdfGenerationOptions();

                // Activer le post-processing pour la numérotation globale et les pieds de page uniformes
                options.DisableFooterForPostProcessing = true;
                _loggingService.LogInformation("Post-processing activé : Les pieds de page seront ajoutés avec numérotation globale après assembly");

                // 1. Page de garde
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.PageDeGarde);
                if (document.IncludePageDeGarde && document.Chantier != null)
                {
                    var pageDeGarde = await GeneratePageDeGardeAsync(document, GetTypeDocumentLabel(document.TypeDocument), options);
                    pdfParts.Add(pageDeGarde);
                }

                // 2. Table des matières (sera générée après analyse du contenu)
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.AnalyseTableMatieres);
                TableOfContentsData? tocData = null;
                if (document.IncludeTableMatieres)
                {
                    tocData = await BuildTableOfContentsAsync(document);
                }

                // 3. Sections libres
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.SectionsLibres);
                if (document.SectionsConteneurs?.Any() == true)
                {
                    foreach (var container in document.SectionsConteneurs.OrderBy(sc => sc.Ordre))
                    {
                        if (container.Items?.Any() == true)
                        {
                            var htmlContent = await BuildSectionHtmlAsync(container);
                            var sectionPdf = await ConvertHtmlToPdfAsync(htmlContent, options, document);
                            pdfParts.Add(sectionPdf);
                        }
                    }
                }

                // 4. Tableau de synthèse des produits (si activé) - juste avant les fiches techniques
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.TableauSynthese);
                if (document.FTConteneur?.AfficherTableauRecapitulatif == true &&
                    document.FTConteneur?.Elements?.Any() == true)
                {
                    _loggingService.LogInformation("Génération du tableau de synthèse des produits");

                    var tableauHtml = await _htmlTemplateService.GenerateTableauSyntheseProduits(document.FTConteneur);
                    var tableauPdf = await ConvertHtmlToPdfAsync(tableauHtml, options, document);
                    pdfParts.Add(tableauPdf);

                    _loggingService.LogInformation("Tableau de synthèse ajouté avant les fiches techniques");
                }

                // 5. Fiches techniques (PDFs existants)
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.FichesTechniques);
                if (document.FTConteneur?.Elements?.Any() == true)
                {
                    var totalElements = document.FTConteneur.Elements.Count;
                    var processedElements = 0;

                    foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
                    {
                        if (element.ImportPDF?.CheminFichier != null && File.Exists(element.ImportPDF.CheminFichier))
                        {
                            var existingPdfBytes = await File.ReadAllBytesAsync(element.ImportPDF.CheminFichier);
                            pdfParts.Add(existingPdfBytes);
                        }

                        processedElements++;
                        // Mise à jour du message avec le nombre de fiches traitées
                        _progressService.UpdateProgress(document.Id, PdfGenerationStep.FichesTechniques,
                            $"Intégration des fiches techniques ({processedElements}/{totalElements})");
                    }
                }

                // 6. Insertion de la table des matières si nécessaire
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.InsertionTableMatieres);
                if (tocData != null && document.IncludeTableMatieres)
                {
                    var tocPdf = await GenerateTableMatieresAsync(tocData, document, options);
                    pdfParts.Insert(document.IncludePageDeGarde ? 1 : 0, tocPdf);
                }

                // 7. Assembly final avec post-processing des pieds de page
                _progressService.UpdateProgress(document.Id, PdfGenerationStep.AssemblyFinal);
                var assemblyOptions = new PdfAssemblyOptions
                {
                    AddBookmarks = true,
                    AddPageNumbers = false, // Désactivé car géré par post-processing
                    OptimizeForPrint = true,
                    EnableFooterPostProcessing = true,
                    DocumentForFooter = document
                };

                var finalPdf = await AssemblePdfsAsync(pdfParts, assemblyOptions);

                // 8. Optimisation
                var optimizationOptions = new PdfOptimizationOptions
                {
                    CompressImages = true,
                    EmbedFonts = true,
                    Title = $"{GetTypeDocumentLabel(document.TypeDocument)} - {document.Chantier?.NomProjet}",
                    Author = appSettings.NomSociete,
                    Subject = GetTypeDocumentLabel(document.TypeDocument),
                    Keywords = "DOE, Technique, Construction"
                };

                var optimizedPdf = await OptimizePdfAsync(finalPdf, optimizationOptions);

                // Marquer comme terminé
                _progressService.CompleteProgress(document.Id, "PDF généré avec succès !");

                return optimizedPdf;
            }
            catch (Exception ex)
            {
                // Marquer la progression comme échouée
                _progressService.SetError(document.Id, ex.Message);

                _loggingService.LogError(ex, $"Erreur génération PDF: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convertit du contenu HTML en PDF via PuppeteerSharp avec gestion des images et CSS
        /// Configure automatiquement les marges, en-têtes et pieds de page
        /// </summary>
        /// <param name="htmlContent">Contenu HTML complet à convertir</param>
        /// <param name="options">Options de mise en page et de rendu</param>
        /// <param name="document">Document optionnel pour personnaliser le pied de page avec les informations du chantier</param>
        /// <returns>Données binaires du PDF généré</returns>
        public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, PdfGenerationOptions? options = null, DocumentGenere? document = null)
        {
            options ??= new PdfGenerationOptions();
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var browser = await GetBrowserAsync();
            
            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContent);

            // Attendre le chargement complet (images, CSS) - timeout plus long pour les images base64
            await page.WaitForTimeoutAsync(5000);

            // Attendre spécifiquement que les images soient chargées
            try
            {
                await page.WaitForSelectorAsync("img", new WaitForSelectorOptions { Timeout = 3000 });
                _loggingService.LogInformation("Images détectées dans le HTML");
            }
            catch (Exception)
            {
                _loggingService.LogInformation("Aucune image détectée ou timeout d'attente atteint");
            }
            
            // Choisir le template de pied de page approprié (sauf si post-processing activé)
            string footerTemplate = "";
            bool displayFooter = options.DisplayHeaderFooter;

            if (options.DisableFooterForPostProcessing)
            {
                // Post-processing activé : désactiver les pieds de page PuppeteerSharp
                displayFooter = false;
                footerTemplate = "";
                _loggingService.LogInformation("Pied de page désactivé - sera ajouté en post-processing via PDFSharp");
            }
            else
            {
                // Comportement normal : utiliser les templates de pied de page
                footerTemplate = options.FooterTemplate ??
                    (document != null ? GetDocumentFooterTemplate(document, appSettings) : GetDefaultFooterTemplate());
            }

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                DisplayHeaderFooter = displayFooter,
                HeaderTemplate = options.HeaderTemplate ?? GetDefaultHeaderTemplate(appSettings),
                FooterTemplate = footerTemplate,
                PrintBackground = options.PrintBackground,
                MarginOptions = new MarginOptions
                {
                    Top = options.MarginTop,
                    Bottom = options.MarginBottom,
                    Left = options.MarginLeft,
                    Right = options.MarginRight
                },
                Scale = options.Scale
            };

            return await page.PdfDataAsync(pdfOptions);
        }

        /// <summary>
        /// Assemble plusieurs PDFs en un seul document avec gestion des signets et numérotation
        /// Utilise PDFSharp pour fusionner les pages et ajouter les métadonnées
        /// </summary>
        /// <param name="pdfBytesList">Liste des PDFs à assembler sous forme de bytes</param>
        /// <param name="options">Options d'assembly (signets, numérotation, optimisation)</param>
        /// <returns>PDF final assemblé avec toutes les pages et signets</returns>
        public async Task<byte[]> AssemblePdfsAsync(IEnumerable<byte[]> pdfBytesList, PdfAssemblyOptions? options = null)
        {
            options ??= new PdfAssemblyOptions();
            var appSettings = await _configurationService.GetAppSettingsAsync();

            using var outputDocument = new PdfDocument();
            outputDocument.Info.Title = "Document Assemblé";
            outputDocument.Info.Author = appSettings.NomSociete;
            outputDocument.Info.CreationDate = DateTime.Now;
            
            var pageCounter = 0;
            var bookmarks = new List<(string title, int pageIndex)>();

            foreach (var pdfBytes in pdfBytesList)
            {
                using var inputStream = new MemoryStream(pdfBytes);
                using var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                
                var startPage = pageCounter;
                
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    var page = inputDocument.Pages[i];
                    outputDocument.AddPage(page);
                    pageCounter++;
                }

                // Ajouter bookmark si demandé
                if (options.AddBookmarks && inputDocument.PageCount > 0)
                {
                    var title = inputDocument.Info.Title ?? $"Section {bookmarks.Count + 1}";
                    bookmarks.Add((title, startPage));
                }
            }

            // Ajouter les bookmarks
            if (options.AddBookmarks && bookmarks.Any())
            {
                foreach (var bookmark in bookmarks)
                {
                    var outline = outputDocument.Outlines.Add(bookmark.title, outputDocument.Pages[bookmark.pageIndex]);
                }
            }

            // Post-processing : Ajouter les pieds de page avec numérotation globale si activé
            if (options.EnableFooterPostProcessing && options.DocumentForFooter != null)
            {
                _loggingService.LogInformation("Démarrage du post-processing : Ajout des pieds de page avec numérotation globale");
                await AddFooterToAllPagesAsync(outputDocument, options.DocumentForFooter, true);
                _loggingService.LogInformation("Post-processing terminé avec succès");
            }

            using var outputStream = new MemoryStream();
            outputDocument.Save(outputStream);
            return outputStream.ToArray();
        }

        /// <summary>
        /// Génère une page de garde personnalisée avec template configurable et gestion automatique du logo
        /// Supporte les templates personnalisés via PageGardeTemplateService avec fallback vers template par défaut
        /// </summary>
        /// <param name="document">Document contenant les informations projet et chantier</param>
        /// <param name="typeDocument">Type de document (DOE, Dossier Technique, etc.)</param>
        /// <param name="options">Options de génération PDF (marges, format, etc.)</param>
        /// <returns>Page de garde en PDF sans numérotation</returns>
        public async Task<byte[]> GeneratePageDeGardeAsync(DocumentGenere document, string typeDocument, PdfGenerationOptions? options = null)
        {
            string html;

            try
            {
                // Si un template spécifique est sélectionné, l'utiliser
                if (document.PageGardeTemplateId.HasValue)
                {
                    var template = await _pageGardeTemplateService.GetTemplateByIdAsync(document.PageGardeTemplateId.Value);
                    if (template != null)
                    {
                        _loggingService.LogInformation($"Utilisation du template personnalisé {template.Nom} pour la page de garde");
                        html = await _pageGardeTemplateService.CompileTemplateAsync(template, document, typeDocument);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Template {document.PageGardeTemplateId} introuvable, utilisation du template par défaut");
                        html = await GetDefaultPageGardeHtmlAsync(document, typeDocument);
                    }
                }
                else
                {
                    // Essayer d'utiliser le template par défaut du service
                    var defaultTemplate = await _pageGardeTemplateService.GetDefaultTemplateAsync();
                    if (defaultTemplate != null)
                    {
                        _loggingService.LogInformation($"Utilisation du template par défaut {defaultTemplate.Nom} pour la page de garde");
                        html = await _pageGardeTemplateService.CompileTemplateAsync(defaultTemplate, document, typeDocument);
                    }
                    else
                    {
                        _loggingService.LogInformation("Aucun template par défaut configuré, utilisation du template interne");
                        html = await GetDefaultPageGardeHtmlAsync(document, typeDocument);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la génération de la page de garde avec template : {ex.Message}");
                _loggingService.LogInformation("Fallback vers le template interne par défaut");
                html = await GetDefaultPageGardeHtmlAsync(document, typeDocument);
            }

            // Créer des options spéciales pour la page de garde (sans numérotation)
            var pageGardeOptions = new PdfGenerationOptions
            {
                Format = options?.Format ?? "A4",
                DisplayHeaderFooter = false, // Pas de header/footer pour la page de garde
                HeaderTemplate = "", // Template vide
                FooterTemplate = "", // Template vide
                PrintBackground = options?.PrintBackground ?? true,
                MarginTop = options?.MarginTop ?? "10mm",
                MarginBottom = options?.MarginBottom ?? "10mm",
                MarginLeft = options?.MarginLeft ?? "10mm",
                MarginRight = options?.MarginRight ?? "10mm",
                Scale = options?.Scale ?? 1,
                WaitForTimeout = options?.WaitForTimeout ?? 30000
            };

            return await ConvertHtmlToPdfAsync(html, pageGardeOptions);
        }

        /// <summary>
        /// Génère le HTML par défaut pour la page de garde avec styles CSS intégrés
        /// Template de fallback utilisé quand aucun template personnalisé n'est disponible
        /// </summary>
        /// <param name="document">Document avec informations projet</param>
        /// <param name="typeDocument">Titre du type de document</param>
        /// <returns>HTML complet de la page de garde avec styles CSS</returns>
        private async Task<string> GetDefaultPageGardeHtmlAsync(DocumentGenere document, string typeDocument)
        {
            _loggingService.LogInformation("Génération page de garde avec template par défaut intégré (sans gestion logo automatique)");
            var appSettings = await _configurationService.GetAppSettingsAsync();

            return $@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <style>
                    {CssStylesHelper.GetCoverPageCSS()}
                    .main-title {{
                        font-size: 2.2em;
                        font-weight: 600;
                        margin-bottom: 25px;
                        text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
                        font-family: Arial, sans-serif;
                    }}
                    .project-info {{
                        background: rgba(255,255,255,0.1);
                        padding: 25px;
                        border-radius: 10px;
                        margin: 20px 0;
                        backdrop-filter: blur(10px);
                        border: 1px solid rgba(255,255,255,0.2);
                    }}
                    .info-row {{
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        margin: 10px 0;
                        font-size: 1.0em;
                        font-family: Arial, sans-serif;
                    }}
                    .label {{
                        font-weight: 600;
                        font-family: Arial, sans-serif;
                        min-width: 140px;
                    }}
                    .value {{
                        font-family: Arial, sans-serif;
                        text-align: right;
                        flex: 1;
                    }}
                    .company-info {{
                        margin-top: 30px;
                        font-size: 1.1em;
                        opacity: 0.9;
                        font-family: Arial, sans-serif;
                        font-weight: 500;
                    }}
                    .date {{
                        position: absolute;
                        bottom: 20px;
                        right: 40px;
                        font-size: 0.9em;
                        opacity: 0.8;
                        font-family: Arial, sans-serif;
                    }}
                    /* Suppression de marges par défaut pour éviter débordement */
                    * {{
                        margin: 0;
                        padding: 0;
                    }}
                </style>
            </head>
            <body>
                <h1 class='main-title'>{typeDocument}</h1>

                <div class='project-info'>
                    <div class='info-row'>
                        <span class='label'>Projet :</span>
                        <span class='value'>{document.Chantier?.NomProjet ?? "Non défini"}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Maître d'œuvre :</span>
                        <span class='value'>{document.Chantier?.MaitreOeuvre ?? "Non défini"}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Maître d'ouvrage :</span>
                        <span class='value'>{document.Chantier?.MaitreOuvrage ?? "Non défini"}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Adresse :</span>
                        <span class='value'>{document.Chantier?.Adresse ?? "Non défini"}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Lot :</span>
                        <span class='value'>{document.NumeroLot} - {document.IntituleLot}</span>
                    </div>
                </div>

                <div class='company-info'>
                    <div style='display: flex; align-items: center; justify-content: center; margin-bottom: 15px;'>
                        <strong>{appSettings.NomSociete}</strong>
                    </div>
                </div>

                <div class='date'>
                    {DateTime.Now:dd/MM/yyyy}
                </div>
            </body>
            </html>";
        }

        /// <summary>
        /// Recherche automatiquement un logo dans le répertoire d'images configuré
        /// Utilise des patterns de recherche (logo*, titre*, illustration*) avec fallback vers favicon
        /// </summary>
        /// <returns>URL du logo trouvé ou chaîne vide si aucun logo disponible</returns>
        private async Task<string> GetLogoUrlAsync()
        {
            try
            {
                // Utiliser le répertoire d'images configuré
                var appSettings = await _configurationService.GetAppSettingsAsync();
                var imagesDirectory = appSettings.RepertoireStockageImages;
                _loggingService.LogInformation($"Recherche de logo dans le répertoire configuré : {imagesDirectory}");

                if (Directory.Exists(imagesDirectory))
                {
                    // Chercher d'abord un fichier "logo" ou "titre" (plus récent en premier)
                    var logoPatterns = new[] { "*logo*", "*titre*", "*Titre*", "*illustration*" };
                    var imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };

                    foreach (var pattern in logoPatterns)
                    {
                        foreach (var extension in imageExtensions)
                        {
                            var searchPattern = pattern + Path.GetExtension(extension);
                            var logoFiles = Directory.GetFiles(imagesDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                                                   .OrderByDescending(f => File.GetLastWriteTime(f))
                                                   .ToArray();

                            if (logoFiles.Any())
                            {
                                var logoPath = logoFiles.First();
                                var fileName = Path.GetFileName(logoPath);
                                _loggingService.LogInformation($"Logo trouvé : {fileName}");

                                // Construire l'URL vers l'API d'images avec détection automatique du port
                                var logoUrl = $"{GetBaseUrl()}/api/images/{fileName}";
                                _loggingService.LogInformation($"URL logo générée dynamiquement : {logoUrl}");
                                return logoUrl;
                            }
                        }
                    }

                    _loggingService.LogWarning($"Aucun fichier logo trouvé dans {imagesDirectory} avec les patterns : {string.Join(", ", logoPatterns)}");
                }
                else
                {
                    _loggingService.LogWarning($"Répertoire d'images non trouvé : {imagesDirectory}");
                }

                // Fallback vers le favicon depuis wwwroot si aucun logo n'est trouvé
                var faviconPath = Path.Combine(_webHostEnvironment.WebRootPath, "favicon-32x32.png");
                if (File.Exists(faviconPath))
                {
                    var faviconUrl = $"{GetBaseUrl()}/favicon-32x32.png";
                    _loggingService.LogInformation($"Fallback vers favicon avec URL dynamique : {faviconUrl}");
                    return faviconUrl;
                }

                _loggingService.LogWarning("Aucun logo disponible - page de garde sans logo");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la recherche du logo : {ex.Message}");
                return string.Empty;
            }
        }


        /// <summary>
        /// Génère une table des matières formatée avec configuration personnalisable
        /// Extrait les paramètres depuis le JSON du document (titre, numérotation, style)
        /// </summary>
        /// <param name="tocData">Données structurées de la table des matières</param>
        /// <param name="document">Document contenant la configuration de la table des matières</param>
        /// <param name="options">Options de génération PDF</param>
        /// <returns>Table des matières en PDF avec styles et hiérarchie</returns>
        public async Task<byte[]> GenerateTableMatieresAsync(TableOfContentsData tocData, DocumentGenere document, PdfGenerationOptions? options = null)
        {
            // Extraire les paramètres de table des matières depuis le JSON
            string titreTableMatieres = "Table des matières";
            bool includeNumeroPages = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(document.Parametres))
                {
                    var parametres = JsonSerializer.Deserialize<JsonElement>(document.Parametres);
                    if (parametres.TryGetProperty("TableMatieres", out var tableMatieres))
                    {
                        if (tableMatieres.TryGetProperty("Titre", out var titre))
                        {
                            titreTableMatieres = titre.GetString() ?? "Table des matières";
                        }
                        if (tableMatieres.TryGetProperty("IncludeNumeroPages", out var includePages))
                        {
                            includeNumeroPages = includePages.GetBoolean();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Erreur lors de la lecture des paramètres de table des matières : {ex.Message}. Utilisation des valeurs par défaut.");
            }

            var html = new StringBuilder();
            html.AppendLine(@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <style>
                    @page {
                        size: A4;
                        margin: 10mm;
                    }
                    body {
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        margin: 40px;
                        line-height: 1.6;
                    }
                    .toc-title {
                        font-size: 2.5em;
                        color: #2c3e50;
                        border-bottom: 3px solid #3498db;
                        padding-bottom: 20px;
                        margin-bottom: 40px;
                    }
                    .toc-entry {
                        display: flex;
                        justify-content: space-between;
                        margin: 10px 0;
                        padding: 8px 0;
                        border-bottom: 1px dotted #ddd;
                    }
                    .toc-entry.level-1 { font-size: 1.1em; font-weight: 600; }
                    .toc-entry.level-2 { margin-left: 20px; }
                    .toc-entry.level-3 { margin-left: 40px; font-size: 0.9em; }
                    .page-number { font-weight: bold; color: #3498db; }
                    .toc-entry-no-pages { justify-content: flex-start; }
                </style>
            </head>
            <body>
                <h1 class='toc-title'>" + titreTableMatieres + @"</h1>");

            foreach (var entry in tocData.Entries)
            {
                await AppendTocEntryAsync(html, entry, 1, includeNumeroPages);
            }

            html.AppendLine("</body></html>");

            return await ConvertHtmlToPdfAsync(html.ToString(), options, document);
        }

        /// <summary>
        /// Optimise un PDF en ajoutant les métadonnées et en appliquant les options de compression
        /// Utilise PDFSharp pour modifier les propriétés du document (titre, auteur, mots-clés)
        /// </summary>
        /// <param name="pdfBytes">PDF source à optimiser</param>
        /// <param name="options">Options d'optimisation (compression, métadonnées, PDF/A)</param>
        /// <returns>PDF optimisé avec métadonnées mises à jour</returns>
        public async Task<byte[]> OptimizePdfAsync(byte[] pdfBytes, PdfOptimizationOptions? options = null)
        {
            options ??= new PdfOptimizationOptions();
            
            using var inputStream = new MemoryStream(pdfBytes);
            using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);
            
            // Métadonnées
            document.Info.Title = options.Title;
            document.Info.Author = options.Author;
            document.Info.Subject = options.Subject;
            document.Info.Keywords = options.Keywords;
            document.Info.CreationDate = DateTime.Now;
            
            // TODO: Implémenter compression images et optimisations avancées
            
            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            
            await Task.CompletedTask;
            return outputStream.ToArray();
        }

        /// <summary>
        /// Construit les données structurées de la table des matières en analysant le document
        /// Calcule les numéros de page estimés pour chaque section et fiche technique
        /// </summary>
        /// <param name="document">Document à analyser pour extraire la structure</param>
        /// <returns>Données hiérarchisées de la table des matières avec numéros de page</returns>
        private async Task<TableOfContentsData> BuildTableOfContentsAsync(DocumentGenere document)
        {
            // Vérifier si le document utilise une table des matières personnalisée
            var customTocConfig = ExtractCustomTocConfiguration(document);
            _loggingService.LogInformation($"🔍 Configuration TOC extraite - Document {document.Id}:");

            if (customTocConfig != null)
            {
                _loggingService.LogInformation($"  ✅ Configuration trouvée - Mode: {customTocConfig.ModeGeneration}, Entrées: {customTocConfig.EntriesCustom.Count}");

                if (customTocConfig.ModeGeneration == CustomModeGeneration.Personnalisable && customTocConfig.EntriesCustom.Any())
                {
                    _loggingService.LogInformation($"  🎯 Utilisation de la table des matières PERSONNALISÉE avec {customTocConfig.EntriesCustom.Count} entrées");
                    return await BuildCustomTableOfContentsAsync(document, customTocConfig);
                }
                else
                {
                    _loggingService.LogInformation($"  ⚠️ Mode non personnalisable ou pas d'entrées personnalisées - Utilisation du mode automatique");
                }
            }
            else
            {
                _loggingService.LogInformation($"  ❌ Aucune configuration personnalisée trouvée - Utilisation du mode automatique");
            }

            // Mode automatique (comportement original)
            var tocData = new TableOfContentsData();
            var pageNumber = 1;

            // Page de garde
            if (document.IncludePageDeGarde)
                pageNumber++;

            // Table des matières elle-même
            pageNumber++;

            // Sections libres
            if (document.SectionsConteneurs?.Any() == true)
            {
                foreach (var container in document.SectionsConteneurs.OrderBy(sc => sc.Ordre))
                {
                    var entry = new TocEntry
                    {
                        Title = container.Titre,
                        Level = 1,
                        PageNumber = pageNumber
                    };

                    if (container.Items?.Any() == true)
                    {
                        foreach (var section in container.Items.OrderBy(sl => sl.Ordre))
                        {
                            entry.Children.Add(new TocEntry
                            {
                                Title = section.SectionLibre.Titre,
                                Level = 2,
                                PageNumber = pageNumber
                            });
                        }
                    }

                    tocData.Entries.Add(entry);
                    pageNumber += 2; // Estimation
                }
            }

            // Tableau de synthèse des produits (si activé)
            if (document.FTConteneur?.AfficherTableauRecapitulatif == true &&
                document.FTConteneur?.Elements?.Any() == true)
            {
                var syntheseEntry = new TocEntry
                {
                    Title = "Tableau de Synthèse des Produits",
                    Level = 1,
                    PageNumber = pageNumber
                };
                tocData.Entries.Add(syntheseEntry);
                pageNumber += 1; // Une page pour le tableau de synthèse
            }

            // Fiches techniques avec calcul précis des pages PDF
            if (document.FTConteneur?.Elements?.Any() == true)
            {
                var ftEntry = new TocEntry
                {
                    Title = document.FTConteneur.Titre,
                    Level = 1,
                    PageNumber = pageNumber
                };

                // Précharger le cache pour tous les fichiers PDF
                var pdfPaths = document.FTConteneur.Elements
                    .Where(e => e.ImportPDF != null)
                    .Select(e => e.ImportPDF.CheminFichier)
                    .ToList();

                if (pdfPaths.Any())
                {
                    await _pdfPageCountService.PreloadCacheAsync(pdfPaths);
                }

                foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
                {
                    var title = element.FicheTechnique?.NomProduit ?? element.ImportPDF?.NomFichierOriginal ?? "Document";
                    ftEntry.Children.Add(new TocEntry
                    {
                        Title = title,
                        Level = 2,
                        PageNumber = pageNumber
                    });

                    // Calculer le nombre exact de pages pour ce PDF
                    var pdfPageCount = 1; // Par défaut
                    if (element.ImportPDF != null)
                    {
                        var count = await _pdfPageCountService.GetPageCountAsync(element.ImportPDF.CheminFichier);
                        pdfPageCount = count ?? 1; // Si erreur, estimation à 1 page

                        // Mettre à jour la base de données si nécessaire
                        if (count.HasValue && element.ImportPDF.PageCount != count.Value)
                        {
                            element.ImportPDF.PageCount = count.Value;
                            // La sauvegarde sera gérée par le service appelant
                        }
                    }

                    pageNumber += pdfPageCount;
                }

                tocData.Entries.Add(ftEntry);
            }

            await Task.CompletedTask;
            return tocData;
        }

        /// <summary>
        /// Convertit les URLs relatives des images en URLs absolues dans le contenu HTML
        /// Transforme src="/images/nom.jpg" en src="http://localhost:5283/images/nom.jpg"
        /// </summary>
        /// <param name="htmlContent">Contenu HTML contenant potentiellement des images</param>
        /// <returns>HTML avec URLs d'images converties en absolues</returns>
        private string ConvertRelativeImagesToAbsolute(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return htmlContent;

            var baseUrl = GetBaseUrl();
            var pattern = @"src\s*=\s*[""']/images/([^""']+)[""']";
            var replacement = $"src=\"{baseUrl}/images/$1\"";

            var convertedHtml = Regex.Replace(htmlContent, pattern, replacement, RegexOptions.IgnoreCase);

            // Log pour diagnostic
            var matches = Regex.Matches(htmlContent, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                _loggingService.LogInformation($"Conversion de {matches.Count} URLs d'images relatives vers URLs absolues avec base {baseUrl}");
                foreach (Match match in matches)
                {
                    _loggingService.LogInformation($"  Image convertie : /images/{match.Groups[1].Value} → {baseUrl}/images/{match.Groups[1].Value}");
                }
            }

            return convertedHtml;
        }

        /// <summary>
        /// Construit le HTML d'un conteneur de sections libres avec styles CSS professionnels centralisés
        /// Utilise la configuration des styles PDF de l'utilisateur pour personnaliser l'apparence
        /// </summary>
        /// <param name="container">Conteneur de sections avec ses éléments ordonnés</param>
        /// <returns>HTML complet du conteneur avec styles configurables</returns>
        private async Task<string> BuildSectionHtmlAsync(SectionConteneur container)
        {
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <style>
                    {CssStylesHelper.GetSectionLibreCSS(appSettings.StylesPDF)}
                </style>
            </head>
            <body>
                <h1>{container.Titre}</h1>");

            if (container.Items?.Any() == true)
            {
                foreach (var section in container.Items.OrderBy(sl => sl.Ordre))
                {
                    // Convertir les URLs relatives des images en URLs absolues
                    var convertedContent = ConvertRelativeImagesToAbsolute(section.SectionLibre.ContenuHtml);

                    html.AppendLine($@"
                    <div class='section'>
                        <div class='section-title'>{section.SectionLibre.Titre}</div>
                        <div class='section-content'>{convertedContent}</div>
                    </div>");
                }
            }

            html.AppendLine("</body></html>");

            await Task.CompletedTask;
            return html.ToString();
        }

        /// <summary>
        /// Ajoute récursivement une entrée de table des matières au HTML avec gestion de la hiérarchie
        /// Supporte plusieurs niveaux d'indentation et l'affichage optionnel des numéros de page
        /// </summary>
        /// <param name="html">StringBuilder pour construire le HTML</param>
        /// <param name="entry">Entrée à ajouter avec ses enfants potentiels</param>
        /// <param name="level">Niveau d'indentation (1, 2, 3...)</param>
        /// <param name="includeNumeroPages">True pour afficher les numéros de page</param>
        private async Task AppendTocEntryAsync(StringBuilder html, TocEntry entry, int level, bool includeNumeroPages = true)
        {
            if (includeNumeroPages)
            {
                html.AppendLine($@"
            <div class='toc-entry level-{level}'>
                <span>{entry.Title}</span>
                <span class='page-number'>{entry.PageNumber}</span>
            </div>");
            }
            else
            {
                html.AppendLine($@"
            <div class='toc-entry toc-entry-no-pages level-{level}'>
                <span>{entry.Title}</span>
            </div>");
            }

            foreach (var child in entry.Children)
            {
                await AppendTocEntryAsync(html, child, level + 1, includeNumeroPages);
            }
        }

        /// <summary>
        /// Génère le template HTML par défaut pour l'en-tête des pages PDF
        /// Affiche le nom de la société centré avec style minimal
        /// </summary>
        /// <returns>Template HTML pour l'en-tête avec styles inline</returns>
        private string GetDefaultHeaderTemplate(AppSettings appSettings)
        {
            return $@"
            <div style='font-size: 10px; width: 100%; text-align: center; color: #666; margin-top: 10px;'>
                {appSettings.NomSociete}
            </div>";
        }

        /// <summary>
        /// Génère le template HTML par défaut pour le pied de page des PDF
        /// Affiche la numérotation des pages au format "X / Y"
        /// </summary>
        /// <returns>Template HTML pour le pied de page avec numérotation automatique</returns>
        private string GetDefaultFooterTemplate()
        {
            return @"
            <div style='font-size: 10px; width: 100%; text-align: center; color: #666; margin-bottom: 10px;'>
                <span class='pageNumber'></span> / <span class='totalPages'></span>
            </div>";
        }

        /// <summary>
        /// Génère le template HTML personnalisé pour le pied de page des PDF avec informations du document
        /// Affiche le nom du chantier et type de document à gauche, numérotation à droite
        /// </summary>
        /// <param name="document">Document contenant les informations du chantier</param>
        /// <param name="appSettings">Configuration de l'application</param>
        /// <returns>Template HTML avec informations personnalisées et alignement gauche/droite</returns>
        private string GetDocumentFooterTemplate(DocumentGenere document, AppSettings appSettings)
        {
            var nomChantier = document.Chantier?.NomProjet ?? "Chantier";
            var typeDocument = GetTypeDocumentLabel(document.TypeDocument);

            return $@"
            <div style='font-size: 10px; width: 100%; display: flex; justify-content: space-between; align-items: center; color: #666; margin-bottom: 10px; padding: 0 5px;'>
                <span style='flex: 1; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;'>{nomChantier} - {typeDocument}</span>
                <span style='white-space: nowrap; margin-left: 10px;'><span class='pageNumber'></span> / <span class='totalPages'></span></span>
            </div>";
        }

        /// <summary>
        /// Convertit le type de document énuméré en libellé lisible pour l'affichage
        /// </summary>
        /// <param name="typeDocument">Type de document énuméré</param>
        /// <returns>Libellé complet du type de document en français</returns>
        private string GetTypeDocumentLabel(TypeDocumentGenere typeDocument)
        {
            return typeDocument switch
            {
                TypeDocumentGenere.DOE => "Dossier d'Ouvrages Exécutés",
                TypeDocumentGenere.DossierTechnique => "Dossier Technique",
                TypeDocumentGenere.MemoireTechnique => "Mémoire Technique",
                _ => "Document"
            };
        }

        /// <summary>
        /// Extrait la configuration personnalisée de la table des matières depuis les paramètres JSON
        /// </summary>
        private CustomTableMatieresConfig? ExtractCustomTocConfiguration(DocumentGenere document)
        {
            try
            {
                _loggingService.LogInformation($"🔍 ExtractCustomTocConfiguration - Document {document.Id}");

                if (string.IsNullOrWhiteSpace(document.Parametres))
                {
                    _loggingService.LogInformation($"  ❌ Parametres vide ou null");
                    return null;
                }

                _loggingService.LogInformation($"  📋 Parametres JSON (premières 200 char): {document.Parametres.Substring(0, Math.Min(200, document.Parametres.Length))}...");

                var settings = JsonSerializer.Deserialize<JsonElement>(document.Parametres);

                if (settings.TryGetProperty("TableMatieres", out var tableMatieres))
                {
                    _loggingService.LogInformation($"  ✅ Section TableMatieres trouvée dans JSON");

                    var config = new CustomTableMatieresConfig();

                    if (tableMatieres.TryGetProperty("ModeGeneration", out var mode))
                    {
                        try
                        {
                            // Gérer les deux cas : valeur numérique ou chaîne
                            if (mode.ValueKind == JsonValueKind.Number)
                            {
                                var modeInt = mode.GetInt32();
                                _loggingService.LogInformation($"  🔧 ModeGeneration trouvé (nombre): {modeInt}");
                                config.ModeGeneration = (CustomModeGeneration)modeInt;
                                _loggingService.LogInformation($"  ✅ ModeGeneration parsé depuis nombre: {config.ModeGeneration}");
                            }
                            else if (mode.ValueKind == JsonValueKind.String)
                            {
                                var modeString = mode.GetString();
                                _loggingService.LogInformation($"  🔧 ModeGeneration trouvé (chaîne): {modeString}");
                                if (Enum.TryParse<CustomModeGeneration>(modeString, out var modeEnum))
                                {
                                    config.ModeGeneration = modeEnum;
                                    _loggingService.LogInformation($"  ✅ ModeGeneration parsé depuis chaîne: {modeEnum}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"  ❌ Erreur parsing ModeGeneration: {ex.Message}");
                        }
                    }

                    if (tableMatieres.TryGetProperty("UseAutoPageNumbers", out var useAuto))
                    {
                        config.UseAutoPageNumbers = useAuto.GetBoolean();
                        _loggingService.LogInformation($"  🔧 UseAutoPageNumbers: {config.UseAutoPageNumbers}");
                    }

                    if (tableMatieres.TryGetProperty("EntriesCustom", out var entries))
                    {
                        var entriesJson = entries.GetRawText();
                        _loggingService.LogInformation($"  📋 EntriesCustom JSON: {entriesJson}");

                        config.EntriesCustom = JsonSerializer.Deserialize<List<CustomTocEntry>>(entriesJson) ?? new();
                        _loggingService.LogInformation($"  ✅ {config.EntriesCustom.Count} entrées personnalisées trouvées");
                    }

                    return config;
                }
                else
                {
                    _loggingService.LogInformation($"  ❌ Section TableMatieres non trouvée dans JSON");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"  🔥 Erreur lors de l'extraction de la configuration TOC personnalisée: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Construit une table des matières personnalisée basée sur la configuration utilisateur
        /// </summary>
        private async Task<TableOfContentsData> BuildCustomTableOfContentsAsync(DocumentGenere document, CustomTableMatieresConfig config)
        {
            var tocData = new TableOfContentsData();

            // Trier les entrées personnalisées par ordre
            var sortedEntries = config.EntriesCustom.OrderBy(e => e.Order).ToList();

            // Si les numéros de pages sont automatiques, les recalculer
            if (config.UseAutoPageNumbers)
            {
                var pageNumber = 1;

                // Page de garde
                if (document.IncludePageDeGarde)
                    pageNumber++;

                // Table des matières elle-même
                pageNumber++;

                // Recalculer les numéros de pages pour chaque entrée
                foreach (var entry in sortedEntries)
                {
                    entry.PageNumber = pageNumber;
                    pageNumber += 1; // Estimation simple : 1 page par entrée
                }
            }

            // Convertir les entrées personnalisées vers le format TocEntry
            foreach (var customEntry in sortedEntries)
            {
                var tocEntry = new TocEntry
                {
                    Title = customEntry.Title,
                    Level = customEntry.Level,
                    PageNumber = customEntry.PageNumber
                };

                tocData.Entries.Add(tocEntry);
            }

            // Table des matières personnalisée générée avec {Count} entrées pour le document {DocumentId}
            // tocData.Entries.Count, document.Id

            await Task.CompletedTask;
            return tocData;
        }

        /// <summary>
        /// Ajoute un pied de page uniforme à toutes les pages d'un document PDF assemblé (post-processing)
        /// Utilise PDFSharp pour surimpression avec numérotation globale correcte
        /// </summary>
        /// <param name="document">Document PDF assemblé à modifier</param>
        /// <param name="documentGenere">Document source pour informations du pied de page</param>
        /// <param name="excludePageDeGarde">True pour exclure la page de garde (première page)</param>
        private async Task AddFooterToAllPagesAsync(PdfDocument document, DocumentGenere documentGenere, bool excludePageDeGarde = true)
        {
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var nomChantier = documentGenere.Chantier?.NomProjet ?? "Chantier";
            var typeDocument = GetTypeDocumentLabel(documentGenere.TypeDocument);
            var totalPages = document.PageCount;

            var startPage = excludePageDeGarde ? 1 : 0; // Commencer à la page 2 si on exclut la page de garde

            _loggingService.LogInformation($"Post-processing : Ajout pied de page sur {totalPages - startPage} pages (exclusion page de garde: {excludePageDeGarde})");

            for (int i = startPage; i < totalPages; i++)
            {
                var page = document.Pages[i];
                var pageNumber = i + 1; // Numérotation à partir de 1
                AddFooterToPage(page, pageNumber, totalPages, nomChantier, typeDocument);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Ajoute un pied de page à une page individuelle avec PDFSharp Graphics
        /// Génère le pied de page avec alignement gauche/droite et fond gris clair
        /// </summary>
        /// <param name="page">Page PDF à modifier</param>
        /// <param name="pageNumber">Numéro de la page courante</param>
        /// <param name="totalPages">Nombre total de pages</param>
        /// <param name="nomChantier">Nom du chantier</param>
        /// <param name="typeDocument">Type de document</param>
        private void AddFooterToPage(PdfPage page, int pageNumber, int totalPages, string nomChantier, string typeDocument)
        {
            using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

            // Définir les couleurs et styles
            var font = new PdfSharp.Drawing.XFont("Arial", 8, PdfSharp.Drawing.XFontStyleEx.Regular);
            var textBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(102, 102, 102)); // #666 - texte gris foncé
            var backgroundBrush = new PdfSharp.Drawing.XSolidBrush(PdfSharp.Drawing.XColor.FromArgb(245, 245, 245)); // #F5F5F5 - fond gris clair

            // Position du pied de page
            var pageWidth = page.Width;
            var pageHeight = page.Height;
            var footerHeight = 16; // Hauteur du rectangle de fond
            var footerY = pageHeight - 25; // Position Y du rectangle de fond
            var textY = pageHeight - 16; // Position Y du texte (centré dans le rectangle)
            var leftMargin = 15; // Marge gauche
            var rightMargin = 15; // Marge droite

            // Dessiner le fond gris clair
            var backgroundRect = new PdfSharp.Drawing.XRect(0, footerY, pageWidth, footerHeight);
            gfx.DrawRectangle(backgroundBrush, backgroundRect);

            // Texte de gauche : Nom chantier + Type document
            var leftText = $"{nomChantier} - {typeDocument}";

            // Texte de droite : Numérotation
            var rightText = $"{pageNumber} / {totalPages}";

            // Dessiner le texte de gauche
            gfx.DrawString(leftText, font, textBrush, leftMargin, textY);

            // Mesurer le texte de droite pour l'aligner à droite
            var rightTextSize = gfx.MeasureString(rightText, font);
            var rightX = pageWidth - rightMargin - rightTextSize.Width;

            // Dessiner le texte de droite
            gfx.DrawString(rightText, font, textBrush, rightX, textY);

            _loggingService.LogInformation($"Pied de page avec fond gris clair ajouté à la page {pageNumber}/{totalPages}: '{leftText}' | '{rightText}'");
        }

        /// <summary>
        /// Libère les ressources utilisées par le service (navigateur Chromium et semaphore)
        /// Implémentation de IDisposable pour nettoyage automatique
        /// </summary>
        public void Dispose()
        {
            _browser?.Dispose();
            _browserSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// Configuration pour la table des matières personnalisée
    /// </summary>
    public class CustomTableMatieresConfig
    {
        public CustomModeGeneration ModeGeneration { get; set; } = CustomModeGeneration.Automatique;
        public bool UseAutoPageNumbers { get; set; } = true;
        public List<CustomTocEntry> EntriesCustom { get; set; } = new();
    }

    /// <summary>
    /// Mode de génération de la table des matières
    /// </summary>
    public enum CustomModeGeneration
    {
        Automatique,
        Personnalisable
    }

    /// <summary>
    /// Entrée personnalisée pour la table des matières
    /// </summary>
    public class CustomTocEntry
    {
        public string Title { get; set; } = "";
        public int Level { get; set; } = 1;
        public int PageNumber { get; set; } = 1;
        public bool IsModified { get; set; } = false;
        public string OriginalTitle { get; set; } = "";
        public bool IsManualEntry { get; set; } = false;
        public int Order { get; set; } = 0;
    }
}