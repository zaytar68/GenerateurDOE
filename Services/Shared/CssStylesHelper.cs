using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Shared
{
    /// <summary>
    /// Classe d'aide pour les styles CSS partagés entre services
    /// </summary>
    public static class CssStylesHelper
    {
        /// <summary>
        /// Police standardisée pour les documents PDF
        /// Compatible avec PuppeteerSharp et optimisée pour la génération PDF
        /// </summary>
        public const string StandardFontFamily = "Arial, sans-serif";

        /// <summary>
        /// CSS de base pour tous les documents PDF
        /// </summary>
        public static string GetBaseDocumentCSS()
        {
            return @"
                @page {
                    size: A4;
                    margin: 10mm;
                }
                * {
                    box-sizing: border-box;
                }

                body {
                    font-family: " + StandardFontFamily + @";
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

        /// <summary>
        /// CSS spécialisé pour les pages de garde (sans numérotation)
        /// </summary>
        public static string GetCoverPageCSS()
        {
            return GetBaseDocumentCSS() + @"

                /* Overrides spéciaux pour la page de garde */
                @page {
                    @bottom-center { content: ''; }
                    @bottom-left { content: ''; }
                    @bottom-right { content: ''; }
                    @top-center { content: ''; }
                    @top-left { content: ''; }
                    @top-right { content: ''; }
                }
                * {
                    -webkit-print-color-adjust: exact;
                    color-adjust: exact;
                }
                body {
                    padding: 40px 60px;
                    min-height: calc(297mm - 20mm);
                    max-height: calc(297mm - 20mm);
                    height: calc(297mm - 20mm);
                    display: flex;
                    flex-direction: column;
                    justify-content: center;
                    text-align: center;
                    box-sizing: border-box;
                    overflow: hidden;
                }";
        }

        /// <summary>
        /// CSS configuré pour les sections libres avec paramètres personnalisables
        /// Utilise la configuration PdfStylesConfig pour adapter les styles
        /// </summary>
        /// <param name="config">Configuration des styles personnalisés</param>
        /// <returns>CSS optimisé pour les sections libres avec styles personnalisés</returns>
        public static string GetSectionLibreCSS(PdfStylesConfig config)
        {
            var baseFontSize = config.GetBaseFontSize();
            var printFontSize = config.GetPrintFontSize();

            return $@"
                @page {{
                    size: A4;
                    margin: 10mm;
                }}
                * {{
                    box-sizing: border-box;
                }}

                body {{
                    font-family: {StandardFontFamily};
                    margin: 40px;
                    line-height: {config.LineHeight};
                    color: {config.TextColor};
                    font-size: {baseFontSize}px;
                    background-color: white;
                }}

                h1 {{
                    color: {config.TitleColor};
                    border-bottom: 2px solid {config.BorderColor};
                    padding-bottom: 10px;
                    margin-bottom: 30px;
                    font-size: {baseFontSize * 1.8}px;
                    font-weight: 600;
                    line-height: 1.3;
                }}

                h2 {{
                    color: {config.SubtitleColor};
                    margin-top: 30px;
                    font-size: {baseFontSize * 1.4}px;
                    font-weight: 600;
                    line-height: 1.3;
                }}

                .section {{
                    margin-bottom: 40px;
                    page-break-inside: avoid;
                }}

                .section-title {{
                    font-size: {baseFontSize * 1.3}px;
                    font-weight: 600;
                    color: {config.SubtitleColor};
                    margin-bottom: 15px;
                }}

                .section-content {{
                    color: {config.TextColor};
                    line-height: {config.LineHeight};
                }}

                img {{
                    max-width: 100%;
                    height: auto;
                    margin: 15px 0;
                    page-break-inside: avoid;
                }}

                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 20px 0;
                    page-break-inside: avoid;
                }}

                th, td {{
                    border: 1px solid #ddd;
                    padding: 12px;
                    text-align: left;
                }}

                th {{
                    background-color: #f8f9fa;
                    font-weight: 600;
                    color: {config.TitleColor};
                }}

                p {{
                    margin: 0 0 1em 0;
                    text-align: justify;
                }}

                @media print {{
                    body {{
                        font-size: {printFontSize}px;
                    }}

                    h1, h2, h3 {{
                        page-break-after: avoid;
                    }}

                    img {{
                        max-width: 100% !important;
                        page-break-inside: avoid;
                    }}

                    table {{
                        page-break-inside: avoid;
                    }}
                }}";
        }

        /// <summary>
        /// CSS de base configuré avec les paramètres personnalisés utilisateur
        /// Remplace GetBaseDocumentCSS() avec une version configurable
        /// </summary>
        /// <param name="config">Configuration des styles personnalisés</param>
        /// <returns>CSS de base avec styles personnalisés appliqués</returns>
        public static string GetBaseDocumentCSS(PdfStylesConfig config)
        {
            var baseFontSize = config.GetBaseFontSize();
            var printFontSize = config.GetPrintFontSize();

            return $@"
                @page {{
                    size: A4;
                    margin: 10mm;
                }}
                * {{
                    box-sizing: border-box;
                }}

                body {{
                    font-family: {StandardFontFamily};
                    margin: 0;
                    padding: 0;
                    line-height: {config.LineHeight};
                    color: {config.TextColor};
                    font-size: {baseFontSize}px;
                    background-color: white;
                }}

                h1, h2, h3, h4, h5, h6 {{
                    color: {config.TitleColor};
                    margin-top: 0;
                    margin-bottom: 0.5em;
                    font-weight: 600;
                    line-height: 1.3;
                }}

                h1 {{ font-size: {baseFontSize * 2.5}px; }}
                h2 {{ font-size: {baseFontSize * 2.0}px; }}
                h3 {{ font-size: {baseFontSize * 1.5}px; }}
                h4 {{ font-size: {baseFontSize * 1.3}px; }}
                h5 {{ font-size: {baseFontSize * 1.1}px; }}
                h6 {{ font-size: {baseFontSize}px; }}

                p {{
                    margin: 0 0 1em 0;
                    text-align: justify;
                }}

                a {{
                    color: {config.BorderColor};
                    text-decoration: none;
                }}

                a:hover {{
                    text-decoration: underline;
                }}

                @media print {{
                    body {{
                        font-size: {printFontSize}px;
                    }}

                    h1, h2, h3 {{
                        page-break-after: avoid;
                    }}

                    img {{
                        max-width: 100% !important;
                        page-break-inside: avoid;
                    }}

                    table {{
                        page-break-inside: avoid;
                    }}
                }}";
        }

        /// <summary>
        /// Applique un template prédéfini aux styles
        /// Templates disponibles : Compact, Normal, Large
        /// </summary>
        /// <param name="templateName">Nom du template (Compact, Normal, Large)</param>
        /// <returns>Configuration PdfStylesConfig pour le template demandé</returns>
        public static PdfStylesConfig GetTemplatePredefini(string templateName)
        {
            return templateName switch
            {
                "Compact" => new PdfStylesConfig
                {
                    FontSizeScale = 0.7f,
                    LineHeight = 1.4f,
                    TitleColor = "#2c3e50",
                    SubtitleColor = "#2980b9",
                    TextColor = "#333333",
                    BorderColor = "#3498db",
                    TemplatePredefini = "Compact"
                },
                "Large" => new PdfStylesConfig
                {
                    FontSizeScale = 1.2f,
                    LineHeight = 1.8f,
                    TitleColor = "#2c3e50",
                    SubtitleColor = "#2980b9",
                    TextColor = "#333333",
                    BorderColor = "#3498db",
                    TemplatePredefini = "Large"
                },
                _ => new PdfStylesConfig() // Normal par défaut
            };
        }
    }
}