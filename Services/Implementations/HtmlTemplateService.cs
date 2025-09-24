using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Shared;
using System.Text;
using System.Text.Json;
using static GenerateurDOE.Services.Implementations.CacheServiceExtensions;

namespace GenerateurDOE.Services.Implementations
{
    /// <summary>
    /// Service de génération de templates HTML professionnels pour la création de documents PDF
    /// Gère pages de garde, tables des matières, sections libres et tableaux de synthèse
    /// </summary>
    public class HtmlTemplateService : IHtmlTemplateService
    {
        private readonly ICacheService _cache;
        private readonly IPageGardeTemplateService _pageGardeTemplateService;

        /// <summary>
        /// Initialise une nouvelle instance du service HtmlTemplateService
        /// </summary>
        /// <param name="cache">Service de cache pour optimiser les performances</param>
        /// <param name="pageGardeTemplateService">Service de gestion des templates de page de garde</param>
        public HtmlTemplateService(ICacheService cache, IPageGardeTemplateService pageGardeTemplateService)
        {
            _cache = cache;
            _pageGardeTemplateService = pageGardeTemplateService;
        }
        /// <summary>
        /// Génère le HTML d'une page de garde avec template personnalisable et fallback automatique
        /// Utilise le service PageGardeTemplateService pour les templates avancés
        /// </summary>
        /// <param name="document">Document contenant les informations projet</param>
        /// <param name="typeDocument">Type de document (DOE, Dossier Technique, etc.)</param>
        /// <param name="legacyTemplate">Template hérité (optionnel, déprécié)</param>
        /// <returns>HTML complet de la page de garde avec styles CSS intégrés</returns>
        public async Task<string> GeneratePageDeGardeHtmlAsync(DocumentGenere document, string typeDocument, PageDeGardeTemplate? legacyTemplate = null)
        {
            try
            {
                // Obtenir le template depuis la base de données (template par défaut ou spécifié)
                Models.PageGardeTemplate? pageGardeTemplate = null;

                if (legacyTemplate != null)
                {
                    // Si un ancien template est fourni, on essaie de le convertir ou utiliser le défaut
                    pageGardeTemplate = await _pageGardeTemplateService.GetDefaultTemplateAsync();
                }
                else
                {
                    // Utiliser le template par défaut
                    pageGardeTemplate = await _pageGardeTemplateService.GetDefaultTemplateAsync();
                }

                // Si aucun template n'est trouvé, créer un template minimal
                if (pageGardeTemplate == null)
                {
                    return GenerateFallbackPageDeGarde(document, typeDocument);
                }

                // Compiler le template avec les données du document
                return await _pageGardeTemplateService.CompileTemplateAsync(pageGardeTemplate, document, typeDocument);
            }
            catch (Exception)
            {
                // En cas d'erreur, générer une page de garde de fallback
                return GenerateFallbackPageDeGarde(document, typeDocument);
            }
        }

        /// <summary>
        /// Génère une page de garde de fallback avec design gradient moderne
        /// Utilisée quand aucun template personnalisé n'est disponible ou en cas d'erreur
        /// </summary>
        /// <param name="document">Document avec informations projet</param>
        /// <param name="typeDocument">Type de document pour le titre</param>
        /// <returns>HTML de page de garde avec design moderne et responsive</returns>
        private string GenerateFallbackPageDeGarde(DocumentGenere document, string typeDocument)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    @page {{
                        size: A4;
                        margin: 10mm;
                        @bottom-center {{ content: ''; }}
                        @bottom-left {{ content: ''; }}
                        @bottom-right {{ content: ''; }}
                        @top-center {{ content: ''; }}
                        @top-left {{ content: ''; }}
                        @top-right {{ content: ''; }}
                    }}
                    * {{
                        -webkit-print-color-adjust: exact;
                        color-adjust: exact;
                    }}
                    body {{
                        font-family: Arial, 'Helvetica Neue', Helvetica, sans-serif;
                        margin: 0;
                        padding: 60px;
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        color: white;
                        height: 100vh;
                        display: flex;
                        flex-direction: column;
                        justify-content: center;
                        text-align: center;
                        box-sizing: border-box;
                    }}
                    .main-title {{
                        font-size: 3em;
                        font-weight: 300;
                        margin-bottom: 40px;
                        text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
                        line-height: 1.2;
                    }}
                    .project-info {{
                        background: rgba(255,255,255,0.1);
                        padding: 40px;
                        border-radius: 15px;
                        margin: 40px 0;
                        backdrop-filter: blur(10px);
                        border: 1px solid rgba(255,255,255,0.2);
                    }}
                    .info-row {{
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        margin: 15px 0;
                        font-size: 1.1em;
                        flex-wrap: wrap;
                    }}
                    .info-row .label {{
                        font-weight: 600;
                        margin-right: 20px;
                        min-width: 150px;
                        text-align: left;
                    }}
                    .info-row .value {{
                        flex: 1;
                        text-align: right;
                        word-break: break-word;
                    }}
                    .company-info {{
                        margin-top: 60px;
                        font-size: 1.3em;
                        font-weight: 500;
                        opacity: 0.9;
                    }}
                    .date {{
                        position: absolute;
                        bottom: 40px;
                        right: 40px;
                        font-size: 1em;
                        opacity: 0.8;
                        font-weight: 300;
                    }}
                    @media print {{
                        body {{
                            height: 297mm;
                            width: 210mm;
                            padding: 20mm;
                        }}
                        .date {{
                            bottom: 20mm;
                            right: 20mm;
                        }}
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
                    <strong>Réalisé par notre société</strong>
                </div>

                <div class='date'>
                    {DateTime.Now:dd/MM/yyyy}
                </div>
            </body>
            </html>";
        }

        /// <summary>
        /// Génère le HTML d'une table des matières avec design professionnel et hiérarchie visuelle
        /// Supporte plusieurs niveaux d'indentation et personnalisation via TocTemplate
        /// </summary>
        /// <param name="tocData">Données structurées de la table des matières</param>
        /// <param name="template">Template de style personnalisé (couleurs, police, etc.)</param>
        /// <returns>HTML complet de la table des matières avec styles CSS avancés</returns>
        public async Task<string> GenerateTableMatieresHtmlAsync(TableOfContentsData tocData, TocTemplate? template = null)
        {
            template ??= new TocTemplate();

            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    {GetDefaultDocumentCSS()}
                    
                    .toc-title {{
                        font-size: 2.5em;
                        color: {template.TitleColor};
                        border-bottom: 3px solid {template.BorderColor};
                        padding-bottom: 20px;
                        margin-bottom: 40px;
                        text-align: center;
                    }}
                    .toc-entry {{
                        display: flex;
                        justify-content: space-between;
                        align-items: baseline;
                        margin: 10px 0;
                        padding: 8px 0;
                        {(template.ShowDots ? $"border-bottom: 1px dotted {template.DotColor};" : "")}
                        page-break-inside: avoid;
                    }}
                    .toc-entry.level-1 {{ 
                        font-size: 1.1em; 
                        font-weight: 600;
                        margin-top: 20px;
                    }}
                    .toc-entry.level-2 {{ 
                        margin-left: 30px;
                        font-size: 1em;
                    }}
                    .toc-entry.level-3 {{ 
                        margin-left: 60px; 
                        font-size: 0.9em;
                        font-style: italic;
                    }}
                    .toc-title-text {{
                        flex: 1;
                        padding-right: 20px;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }}
                    .page-number {{ 
                        font-weight: bold; 
                        color: {template.PageNumberColor};
                        white-space: nowrap;
                        min-width: 40px;
                        text-align: right;
                    }}
                </style>
            </head>
            <body>
                <h1 class='toc-title'>Table des Matières</h1>
                <div class='toc-container'>");

            foreach (var entry in tocData.Entries)
            {
                await AppendTocEntryHtmlAsync(html, entry, 1);
            }

            html.AppendLine(@"
                </div>
            </body>
            </html>");

            return html.ToString();
        }

        /// <summary>
        /// Génère le HTML d'un conteneur de sections libres avec mise en page professionnelle
        /// Inclut gestion des images, tableaux, listes et formatage du contenu HTML éditeur
        /// </summary>
        /// <param name="sectionConteneur">Conteneur avec sections ordonnées</param>
        /// <param name="template">Template de style pour personnaliser l'apparence</param>
        /// <param name="stylesConfig">Configuration personnalisée des styles PDF (optionnel)</param>
        /// <returns>HTML formaté avec styles CSS pour impression PDF optimisée</returns>
        public async Task<string> GenerateSectionLibreHtmlAsync(SectionConteneur sectionConteneur, SectionTemplate? template = null, PdfStylesConfig? stylesConfig = null)
        {
            template ??= new SectionTemplate();

            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    {GetDefaultDocumentCSS()}
                    
                    .section-container {{
                        max-width: 800px;
                        margin: 0 auto;
                        padding: 40px;
                    }}
                    .main-title {{ 
                        color: {template.TitleColor};
                        border-bottom: 2px solid {template.BorderColor};
                        padding-bottom: 15px;
                        margin-bottom: 40px;
                        font-size: 2.5em;
                        font-weight: 300;
                    }}
                    .section {{ 
                        margin-bottom: 50px; 
                        page-break-inside: avoid;
                    }}
                    .section-title {{ 
                        font-size: 1.4em;
                        font-weight: 600;
                        color: {template.SubtitleColor};
                        margin-bottom: 20px;
                        padding-bottom: 8px;
                        border-bottom: 1px solid rgba(52, 152, 219, 0.3);
                    }}
                    .section-content {{
                        color: {template.TextColor};
                        line-height: 1.7;
                    }}
                    .section-content img {{ 
                        max-width: 100%; 
                        height: auto; 
                        margin: 15px 0;
                        border-radius: 4px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                    }}
                    .section-content table {{ 
                        width: 100%; 
                        border-collapse: collapse; 
                        margin: 20px 0;
                        font-size: 0.95em;
                    }}
                    .section-content th, 
                    .section-content td {{ 
                        border: 1px solid #ddd; 
                        padding: 12px; 
                        text-align: left; 
                    }}
                    .section-content th {{ 
                        background-color: #f8f9fa;
                        font-weight: 600;
                        color: #2c3e50;
                    }}
                    .section-content tr:nth-child(even) {{
                        background-color: #f9f9f9;
                    }}
                    .section-content blockquote {{
                        border-left: 4px solid {template.BorderColor};
                        margin: 20px 0;
                        padding: 15px 20px;
                        background-color: #f8f9fa;
                        font-style: italic;
                    }}
                    .section-content ul, 
                    .section-content ol {{
                        padding-left: 25px;
                        margin: 15px 0;
                    }}
                    .section-content li {{
                        margin: 8px 0;
                    }}
                </style>
            </head>
            <body>
                <div class='section-container'>
                    <h1 class='main-title'>{sectionConteneur.Titre}</h1>");

            if (sectionConteneur.Items?.Any() == true)
            {
                foreach (var item in sectionConteneur.Items.OrderBy(i => i.Ordre))
                {
                    html.AppendLine($@"
                    <div class='section'>
                        <div class='section-title'>{item.SectionLibre.Titre}</div>
                        <div class='section-content'>{item.SectionLibre.ContenuHtml}</div>
                    </div>");
                }
            }

            html.AppendLine(@"
                </div>
            </body>
            </html>");

            await Task.CompletedTask;
            return html.ToString();
        }

        /// <summary>
        /// Génère le HTML d'un conteneur de fiches techniques avec design en cartes
        /// Affiche produits, fabricants, descriptions et documents associés de manière structurée
        /// </summary>
        /// <param name="ftConteneur">Conteneur de fiches techniques avec éléments ordonnés</param>
        /// <param name="template">Template pour personnaliser couleurs et layout</param>
        /// <returns>HTML avec design en cartes pour fiches techniques</returns>
        public async Task<string> GenerateFTConteneurHtmlAsync(FTConteneur ftConteneur, FTTemplate? template = null)
        {
            template ??= new FTTemplate();

            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    {GetDefaultDocumentCSS()}
                    
                    .ft-container {{
                        max-width: 800px;
                        margin: 0 auto;
                        padding: 40px;
                    }}
                    .main-title {{
                        color: #2c3e50;
                        border-bottom: 2px solid #3498db;
                        padding-bottom: 15px;
                        margin-bottom: 40px;
                        font-size: 2.5em;
                        font-weight: 300;
                        text-align: center;
                    }}
                    .ft-item {{
                        margin-bottom: 40px;
                        padding: 25px;
                        border: 1px solid {template.BorderColor};
                        border-radius: 8px;
                        background-color: white;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.05);
                        page-break-inside: avoid;
                    }}
                    .ft-header {{
                        background-color: {template.HeaderBackgroundColor};
                        margin: -25px -25px 20px -25px;
                        padding: 20px 25px;
                        border-radius: 8px 8px 0 0;
                        border-bottom: 1px solid {template.BorderColor};
                    }}
                    .ft-title {{
                        font-size: 1.3em;
                        font-weight: 600;
                        color: #2c3e50;
                        margin: 0 0 5px 0;
                    }}
                    .ft-fabricant {{
                        color: #7f8c8d;
                        font-style: italic;
                        margin: 0;
                    }}
                    .ft-content {{
                        display: flex;
                        gap: 20px;
                        align-items: flex-start;
                    }}
                    .ft-details {{
                        flex: 1;
                    }}
                    .ft-detail-row {{
                        margin: 10px 0;
                        display: flex;
                        align-items: center;
                    }}
                    .ft-label {{
                        font-weight: 600;
                        min-width: 120px;
                        color: #34495e;
                    }}
                    .ft-value {{
                        color: #2c3e50;
                    }}
                    .ft-description {{
                        margin-top: 15px;
                        padding: 15px;
                        background-color: #f8f9fa;
                        border-radius: 4px;
                        font-size: 0.95em;
                        line-height: 1.6;
                    }}
                    {(template.ShowThumbnails ? $@"
                    .ft-thumbnail {{
                        width: {template.ThumbnailSize};
                        height: {template.ThumbnailSize};
                        object-fit: cover;
                        border: 1px solid {template.BorderColor};
                        border-radius: 4px;
                        flex-shrink: 0;
                    }}" : "")}
                    .ft-documents {{
                        margin-top: 20px;
                        padding-top: 20px;
                        border-top: 1px solid #ecf0f1;
                    }}
                    .ft-documents h4 {{
                        margin: 0 0 10px 0;
                        color: #34495e;
                        font-size: 1em;
                    }}
                    .ft-doc-list {{
                        list-style: none;
                        padding: 0;
                        margin: 0;
                    }}
                    .ft-doc-item {{
                        padding: 8px 12px;
                        margin: 5px 0;
                        background-color: #ecf0f1;
                        border-radius: 4px;
                        font-size: 0.9em;
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                    }}
                    .ft-doc-type {{
                        background-color: #3498db;
                        color: white;
                        padding: 2px 8px;
                        border-radius: 3px;
                        font-size: 0.8em;
                        font-weight: 500;
                    }}
                </style>
            </head>
            <body>
                <div class='ft-container'>
                    <h1 class='main-title'>{ftConteneur.Titre}</h1>");

            if (ftConteneur.Elements?.Any() == true)
            {
                foreach (var element in ftConteneur.Elements.OrderBy(e => e.Ordre))
                {
                    var fiche = element.FicheTechnique;
                    if (fiche != null)
                    {
                        html.AppendLine($@"
                        <div class='ft-item'>
                            <div class='ft-header'>
                                <h3 class='ft-title'>{fiche.NomProduit}</h3>
                                <p class='ft-fabricant'>{fiche.NomFabricant}</p>
                            </div>
                            <div class='ft-content'>
                                <div class='ft-details'>
                                    <div class='ft-detail-row'>
                                        <span class='ft-label'>Type :</span>
                                        <span class='ft-value'>{fiche.TypeProduit}</span>
                                    </div>");

                        if (!string.IsNullOrEmpty(fiche.Description))
                        {
                            html.AppendLine($@"
                                    <div class='ft-description'>
                                        {fiche.Description}
                                    </div>");
                        }

                        if (fiche.ImportsPDF?.Any() == true)
                        {
                            html.AppendLine(@"
                                    <div class='ft-documents'>
                                        <h4>Documents techniques :</h4>
                                        <ul class='ft-doc-list'>");

                            foreach (var pdf in fiche.ImportsPDF)
                            {
                                html.AppendLine($@"
                                            <li class='ft-doc-item'>
                                                <span>{pdf.NomFichierOriginal}</span>
                                                <span class='ft-doc-type'>{pdf.TypeDocumentImport?.Nom ?? "Non défini"}</span>
                                            </li>");
                            }

                            html.AppendLine("</ul></div>");
                        }

                        html.AppendLine(@"
                                </div>
                            </div>
                        </div>");
                    }
                }
            }

            html.AppendLine(@"
                </div>
            </body>
            </html>");

            await Task.CompletedTask;
            return html.ToString();
        }

        /// <summary>
        /// Génère un tableau de synthèse des produits avec tri automatique par position marché
        /// Présente fabricants, produits, types et spécifications dans un tableau responsive
        /// </summary>
        /// <param name="ftConteneur">Conteneur avec les fiches techniques à synthétiser</param>
        /// <param name="template">Template pour styles du tableau (couleurs, tailles)</param>
        /// <returns>HTML de tableau professionnel avec tri et formatage optimisé</returns>
        public async Task<string> GenerateTableauSyntheseProduits(FTConteneur ftConteneur, TableauSyntheseTemplate? template = null)
        {
            template ??= new TableauSyntheseTemplate();

            var html = new StringBuilder();
            html.AppendLine($@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    {GetDefaultDocumentCSS()}

                    .synthese-container {{
                        max-width: 100%;
                        margin: 0 auto;
                        padding: 40px;
                    }}
                    .main-title {{
                        color: #2c3e50;
                        border-bottom: 2px solid #3498db;
                        padding-bottom: 15px;
                        margin-bottom: 40px;
                        font-size: 2.2em;
                        font-weight: 300;
                        text-align: center;
                    }}
                    .synthese-table {{
                        width: 100%;
                        border-collapse: collapse;
                        margin: 20px 0;
                        font-size: 0.9em;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                        border-radius: 8px;
                        overflow: hidden;
                    }}
                    .synthese-table th {{
                        background-color: {template.HeaderBackgroundColor};
                        color: {template.HeaderTextColor};
                        padding: 15px 12px;
                        text-align: left;
                        font-weight: 600;
                        font-size: 0.95em;
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }}
                    .synthese-table td {{
                        padding: 12px;
                        border-bottom: 1px solid {template.BorderColor};
                        color: {template.TextColor};
                        vertical-align: top;
                    }}
                    .synthese-table tbody tr:nth-child(even) {{
                        background-color: {template.AlternateRowColor};
                    }}
                    .synthese-table tbody tr:hover {{
                        background-color: #e8f4fd;
                    }}
                    .position-cell {{
                        font-weight: 600;
                        color: #2980b9;
                        text-align: center;
                        white-space: nowrap;
                    }}
                    .fabricant-cell {{
                        font-weight: 500;
                        color: #34495e;
                    }}
                    .produit-cell {{
                        font-weight: 500;
                    }}
                    .type-cell {{
                        font-style: italic;
                        color: #7f8c8d;
                    }}
                    .specification-cell {{
                        font-size: 0.85em;
                        line-height: 1.4;
                    }}
                    .no-data {{
                        text-align: center;
                        padding: 40px;
                        color: #95a5a6;
                        font-style: italic;
                    }}
                </style>
            </head>
            <body>
                <div class='synthese-container'>
                    <h1 class='main-title'>Tableau de Synthèse des Produits</h1>");

            if (ftConteneur?.Elements?.Any() == true)
            {
                html.AppendLine(@"
                    <table class='synthese-table'>
                        <thead>
                            <tr>
                                <th style='width: 12%;'>Position Marché</th>
                                <th style='width: 20%;'>Fabricant</th>
                                <th style='width: 28%;'>Produit</th>
                                <th style='width: 18%;'>Type</th>
                                <th style='width: 22%;'>Spécification</th>
                            </tr>
                        </thead>
                        <tbody>");

                // Trier les éléments par position marché puis par ordre
                var elementsTriees = ftConteneur.Elements
                    .OrderBy(e => e.PositionMarche ?? "zzz") // Les positions vides à la fin
                    .ThenBy(e => e.Ordre)
                    .ToList();

                foreach (var element in elementsTriees)
                {
                    var fiche = element.FicheTechnique;
                    if (fiche != null)
                    {
                        var positionMarche = !string.IsNullOrEmpty(element.PositionMarche)
                            ? element.PositionMarche
                            : "-";
                        var specification = !string.IsNullOrEmpty(element.Commentaire)
                            ? System.Web.HttpUtility.HtmlEncode(element.Commentaire)
                            : "-";

                        html.AppendLine($@"
                            <tr>
                                <td class='position-cell'>{positionMarche}</td>
                                <td class='fabricant-cell'>{System.Web.HttpUtility.HtmlEncode(fiche.NomFabricant)}</td>
                                <td class='produit-cell'>{System.Web.HttpUtility.HtmlEncode(fiche.NomProduit)}</td>
                                <td class='type-cell'>{System.Web.HttpUtility.HtmlEncode(fiche.TypeProduit)}</td>
                                <td class='specification-cell'>{specification}</td>
                            </tr>");
                    }
                }

                html.AppendLine(@"
                        </tbody>
                    </table>");
            }
            else
            {
                html.AppendLine(@"
                    <div class='no-data'>
                        <p>Aucun produit à afficher dans le tableau de synthèse.</p>
                    </div>");
            }

            html.AppendLine(@"
                </div>
            </body>
            </html>");

            await Task.CompletedTask;
            return html.ToString();
        }

        /// <summary>
        /// Compile un template HTML en remplaçant les placeholders par les données fournies
        /// Implémentation simple avec sérialisation JSON - extensible vers moteur plus sophistiqué
        /// </summary>
        /// <param name="templateHtml">Template HTML avec placeholders {{DATA}}</param>
        /// <param name="data">Données à injecter dans le template</param>
        /// <returns>HTML compilé avec données injectées</returns>
        public async Task<string> CompileTemplateAsync(string templateHtml, object data)
        {
            // Simple template compilation - peut être étendu avec un moteur de template plus sophistiqué
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            var compiledHtml = templateHtml.Replace("{{DATA}}", json);
            
            await Task.CompletedTask;
            return compiledHtml;
        }

        /// <summary>
        /// Récupère les styles CSS de base pour tous les documents générés
        /// Utilise CssStylesHelper pour garantir la cohérence visuelle
        /// </summary>
        /// <returns>CSS de base commun à tous les templates</returns>
        public string GetDefaultDocumentCSS()
        {
            return CssStylesHelper.GetBaseDocumentCSS();
        }

        /// <summary>
        /// Ajoute récursivement une entrée de table des matières au HTML avec gestion de hiérarchie
        /// Gère l'indentation automatique selon le niveau et affichage des numéros de page
        /// </summary>
        /// <param name="html">StringBuilder pour construire le HTML</param>
        /// <param name="entry">Entrée avec titre, page et enfants potentiels</param>
        /// <param name="level">Niveau d'indentation (1, 2, 3...)</param>
        private async Task AppendTocEntryHtmlAsync(StringBuilder html, TocEntry entry, int level)
        {
            html.AppendLine($@"
            <div class='toc-entry level-{level}'>
                <span class='toc-title-text'>{entry.Title}</span>
                <span class='page-number'>{entry.PageNumber}</span>
            </div>");

            foreach (var child in entry.Children)
            {
                await AppendTocEntryHtmlAsync(html, child, level + 1);
            }
        }
    }
}