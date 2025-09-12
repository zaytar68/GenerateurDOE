using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace GenerateurDOE.Services.Implementations
{
    public class HtmlTemplateService : IHtmlTemplateService
    {
        public async Task<string> GeneratePageDeGardeHtmlAsync(Chantier chantier, string typeDocument, PageDeGardeTemplate? template = null)
        {
            template ??= new PageDeGardeTemplate();

            var html = $@"
            <!DOCTYPE html>
            <html lang='fr'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ 
                        font-family: {template.FontFamily};
                        margin: 0;
                        padding: 60px;
                        background: {template.BackgroundGradient};
                        color: {template.TextColor};
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
                    {(template.ShowLogo && !string.IsNullOrEmpty(template.LogoPath) ? $@"
                    .logo {{
                        max-height: 100px;
                        margin-bottom: 30px;
                        opacity: 0.9;
                    }}" : "")}
                    
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
                {(template.ShowLogo && !string.IsNullOrEmpty(template.LogoPath) ? 
                    $"<img src='{template.LogoPath}' alt='Logo' class='logo' />" : "")}
                
                <h1 class='main-title'>{typeDocument}</h1>
                
                <div class='project-info'>
                    <div class='info-row'>
                        <span class='label'>Projet :</span>
                        <span class='value'>{chantier.NomProjet}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Maître d'œuvre :</span>
                        <span class='value'>{chantier.MaitreOeuvre}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Maître d'ouvrage :</span>
                        <span class='value'>{chantier.MaitreOuvrage}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Adresse :</span>
                        <span class='value'>{chantier.Adresse}</span>
                    </div>
                    <div class='info-row'>
                        <span class='label'>Lot :</span>
                        <span class='value'>{chantier.NumeroLot} - {chantier.IntituleLot}</span>
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

            await Task.CompletedTask;
            return html;
        }

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

        public async Task<string> GenerateSectionLibreHtmlAsync(SectionConteneur sectionConteneur, SectionTemplate? template = null)
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
                                                <span class='ft-doc-type'>{pdf.TypeDocument}</span>
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

        public string GetDefaultDocumentCSS()
        {
            return @"
                * {
                    box-sizing: border-box;
                }
                
                body { 
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    margin: 0;
                    padding: 0;
                    line-height: 1.6;
                    color: #333;
                    font-size: 14px;
                    background-color: white;
                }
                
                h1, h2, h3, h4, h5, h6 {
                    color: #2c3e50;
                    margin-top: 0;
                    margin-bottom: 0.5em;
                    font-weight: 600;
                    line-height: 1.3;
                }
                
                h1 { font-size: 2.5em; }
                h2 { font-size: 2em; }
                h3 { font-size: 1.5em; }
                h4 { font-size: 1.3em; }
                h5 { font-size: 1.1em; }
                h6 { font-size: 1em; }
                
                p {
                    margin: 0 0 1em 0;
                    text-align: justify;
                }
                
                a {
                    color: #3498db;
                    text-decoration: none;
                }
                
                a:hover {
                    text-decoration: underline;
                }
                
                @media print {
                    body {
                        font-size: 12px;
                    }
                    
                    h1, h2, h3 {
                        page-break-after: avoid;
                    }
                    
                    img {
                        max-width: 100% !important;
                        page-break-inside: avoid;
                    }
                    
                    table {
                        page-break-inside: avoid;
                    }
                }";
        }

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