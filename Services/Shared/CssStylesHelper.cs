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
    }
}