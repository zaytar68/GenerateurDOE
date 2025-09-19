using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Shared;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Text;

namespace GenerateurDOE.Services.Implementations
{
    public class PdfGenerationService : IPdfGenerationService, IDisposable
    {
        private readonly AppSettings _appSettings;
        private readonly ILoggingService _loggingService;
        private readonly IPageGardeTemplateService _pageGardeTemplateService;
        private readonly IHtmlTemplateService _htmlTemplateService;
        private IBrowser? _browser;
        private readonly SemaphoreSlim _browserSemaphore = new(1, 1);

        public PdfGenerationService(
            IOptions<AppSettings> appSettings,
            ILoggingService loggingService,
            IPageGardeTemplateService pageGardeTemplateService,
            IHtmlTemplateService htmlTemplateService)
        {
            _appSettings = appSettings.Value;
            _loggingService = loggingService;
            _pageGardeTemplateService = pageGardeTemplateService;
            _htmlTemplateService = htmlTemplateService;
        }

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

        public async Task<byte[]> GenerateCompletePdfAsync(DocumentGenere document, PdfGenerationOptions? options = null)
        {
            _loggingService.LogInformation($"Génération PDF complète pour document {document.Id}");
            
            try
            {
                var pdfParts = new List<byte[]>();
                options ??= new PdfGenerationOptions();

                // 1. Page de garde
                if (document.IncludePageDeGarde && document.Chantier != null)
                {
                    var pageDeGarde = await GeneratePageDeGardeAsync(document, GetTypeDocumentLabel(document.TypeDocument), options);
                    pdfParts.Add(pageDeGarde);
                }

                // 2. Table des matières (sera générée après analyse du contenu)
                TableOfContentsData? tocData = null;
                if (document.IncludeTableMatieres)
                {
                    tocData = await BuildTableOfContentsAsync(document);
                }

                // 3. Sections libres
                if (document.SectionsConteneurs?.Any() == true)
                {
                    foreach (var container in document.SectionsConteneurs.OrderBy(sc => sc.Ordre))
                    {
                        if (container.Items?.Any() == true)
                        {
                            var htmlContent = await BuildSectionHtmlAsync(container);
                            var sectionPdf = await ConvertHtmlToPdfAsync(htmlContent, options);
                            pdfParts.Add(sectionPdf);
                        }
                    }
                }

                // 4. Tableau de synthèse des produits (si activé) - juste avant les fiches techniques
                if (document.FTConteneur?.AfficherTableauRecapitulatif == true &&
                    document.FTConteneur?.Elements?.Any() == true)
                {
                    _loggingService.LogInformation("Génération du tableau de synthèse des produits");

                    var tableauHtml = await _htmlTemplateService.GenerateTableauSyntheseProduits(document.FTConteneur);
                    var tableauPdf = await ConvertHtmlToPdfAsync(tableauHtml, options);
                    pdfParts.Add(tableauPdf);

                    _loggingService.LogInformation("Tableau de synthèse ajouté avant les fiches techniques");
                }

                // 5. Fiches techniques (PDFs existants)
                if (document.FTConteneur?.Elements?.Any() == true)
                {
                    foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
                    {
                        if (element.ImportPDF?.CheminFichier != null && File.Exists(element.ImportPDF.CheminFichier))
                        {
                            var existingPdfBytes = await File.ReadAllBytesAsync(element.ImportPDF.CheminFichier);
                            pdfParts.Add(existingPdfBytes);
                        }
                    }
                }

                // 6. Insertion de la table des matières si nécessaire
                if (tocData != null && document.IncludeTableMatieres)
                {
                    var tocPdf = await GenerateTableMatieresAsync(tocData, options);
                    pdfParts.Insert(document.IncludePageDeGarde ? 1 : 0, tocPdf);
                }

                // 7. Assembly final
                var assemblyOptions = new PdfAssemblyOptions
                {
                    AddBookmarks = true,
                    AddPageNumbers = true,
                    OptimizeForPrint = true
                };

                var finalPdf = await AssemblePdfsAsync(pdfParts, assemblyOptions);

                // 7. Optimisation
                var optimizationOptions = new PdfOptimizationOptions
                {
                    CompressImages = true,
                    EmbedFonts = true,
                    Title = $"{GetTypeDocumentLabel(document.TypeDocument)} - {document.Chantier?.NomProjet}",
                    Author = _appSettings.NomSociete,
                    Subject = GetTypeDocumentLabel(document.TypeDocument),
                    Keywords = "DOE, Technique, Construction"
                };

                return await OptimizePdfAsync(finalPdf, optimizationOptions);
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur génération PDF: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, PdfGenerationOptions? options = null)
        {
            options ??= new PdfGenerationOptions();
            var browser = await GetBrowserAsync();
            
            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContent);

            // Attendre le chargement complet (images, CSS)
            await page.WaitForTimeoutAsync(2000);
            
            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                DisplayHeaderFooter = options.DisplayHeaderFooter,
                HeaderTemplate = options.HeaderTemplate ?? GetDefaultHeaderTemplate(),
                FooterTemplate = options.FooterTemplate ?? GetDefaultFooterTemplate(),
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

        public async Task<byte[]> AssemblePdfsAsync(IEnumerable<byte[]> pdfBytesList, PdfAssemblyOptions? options = null)
        {
            options ??= new PdfAssemblyOptions();
            
            using var outputDocument = new PdfDocument();
            outputDocument.Info.Title = "Document Assemblé";
            outputDocument.Info.Author = _appSettings.NomSociete;
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

            using var outputStream = new MemoryStream();
            outputDocument.Save(outputStream);
            return outputStream.ToArray();
        }

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

        private async Task<string> GetDefaultPageGardeHtmlAsync(DocumentGenere document, string typeDocument)
        {
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
                    <strong>{_appSettings.NomSociete}</strong>
                </div>

                <div class='date'>
                    {DateTime.Now:dd/MM/yyyy}
                </div>
            </body>
            </html>";
        }

        public async Task<byte[]> GenerateTableMatieresAsync(TableOfContentsData tocData, PdfGenerationOptions? options = null)
        {
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
                </style>
            </head>
            <body>
                <h1 class='toc-title'>Table des Matières</h1>");

            foreach (var entry in tocData.Entries)
            {
                await AppendTocEntryAsync(html, entry, 1);
            }

            html.AppendLine("</body></html>");

            return await ConvertHtmlToPdfAsync(html.ToString(), options);
        }

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

        private async Task<TableOfContentsData> BuildTableOfContentsAsync(DocumentGenere document)
        {
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

            // Fiches techniques
            if (document.FTConteneur?.Elements?.Any() == true)
            {
                var ftEntry = new TocEntry
                {
                    Title = document.FTConteneur.Titre,
                    Level = 1,
                    PageNumber = pageNumber
                };

                foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
                {
                    var title = element.FicheTechnique?.NomProduit ?? element.ImportPDF?.NomFichierOriginal ?? "Document";
                    ftEntry.Children.Add(new TocEntry
                    {
                        Title = title,
                        Level = 2,
                        PageNumber = pageNumber
                    });
                    pageNumber += 1; // Estimation
                }

                tocData.Entries.Add(ftEntry);
            }

            await Task.CompletedTask;
            return tocData;
        }

        private async Task<string> BuildSectionHtmlAsync(SectionConteneur container)
        {
            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{ 
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        margin: 40px;
                        line-height: 1.6;
                        color: #333;
                    }}
                    h1 {{ 
                        color: #2c3e50;
                        border-bottom: 2px solid #3498db;
                        padding-bottom: 10px;
                        margin-bottom: 30px;
                    }}
                    h2 {{ color: #34495e; margin-top: 30px; }}
                    .section {{ margin-bottom: 40px; }}
                    .section-title {{ 
                        font-size: 1.3em;
                        font-weight: 600;
                        color: #2980b9;
                        margin-bottom: 15px;
                    }}
                    img {{ max-width: 100%; height: auto; margin: 15px 0; }}
                    table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
                    th {{ background-color: #f8f9fa; }}
                </style>
            </head>
            <body>
                <h1>{container.Titre}</h1>");

            if (container.Items?.Any() == true)
            {
                foreach (var section in container.Items.OrderBy(sl => sl.Ordre))
                {
                    html.AppendLine($@"
                    <div class='section'>
                        <div class='section-title'>{section.SectionLibre.Titre}</div>
                        <div class='section-content'>{section.SectionLibre.ContenuHtml}</div>
                    </div>");
                }
            }

            html.AppendLine("</body></html>");

            await Task.CompletedTask;
            return html.ToString();
        }

        private async Task AppendTocEntryAsync(StringBuilder html, TocEntry entry, int level)
        {
            html.AppendLine($@"
            <div class='toc-entry level-{level}'>
                <span>{entry.Title}</span>
                <span class='page-number'>{entry.PageNumber}</span>
            </div>");

            foreach (var child in entry.Children)
            {
                await AppendTocEntryAsync(html, child, level + 1);
            }
        }

        private string GetDefaultHeaderTemplate()
        {
            return $@"
            <div style='font-size: 10px; width: 100%; text-align: center; color: #666; margin-top: 10px;'>
                {_appSettings.NomSociete}
            </div>";
        }

        private string GetDefaultFooterTemplate()
        {
            return @"
            <div style='font-size: 10px; width: 100%; text-align: center; color: #666; margin-bottom: 10px;'>
                <span class='pageNumber'></span> / <span class='totalPages'></span>
            </div>";
        }

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

        public void Dispose()
        {
            _browser?.Dispose();
            _browserSemaphore?.Dispose();
        }
    }
}