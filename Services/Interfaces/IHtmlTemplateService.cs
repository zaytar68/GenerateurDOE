using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces
{
    public interface IHtmlTemplateService
    {
        /// <summary>
        /// Génère le HTML pour une page de garde
        /// </summary>
        Task<string> GeneratePageDeGardeHtmlAsync(Chantier chantier, string typeDocument, PageDeGardeTemplate? template = null);

        /// <summary>
        /// Génère le HTML pour une table des matières
        /// </summary>
        Task<string> GenerateTableMatieresHtmlAsync(TableOfContentsData tocData, TocTemplate? template = null);

        /// <summary>
        /// Génère le HTML pour une section libre
        /// </summary>
        Task<string> GenerateSectionLibreHtmlAsync(SectionConteneur sectionConteneur, SectionTemplate? template = null);

        /// <summary>
        /// Génère le HTML pour un conteneur de fiches techniques
        /// </summary>
        Task<string> GenerateFTConteneurHtmlAsync(FTConteneur ftConteneur, FTTemplate? template = null);

        /// <summary>
        /// Compile un template HTML avec les données fournies
        /// </summary>
        Task<string> CompileTemplateAsync(string templateHtml, object data);

        /// <summary>
        /// Obtient les CSS par défaut pour les documents
        /// </summary>
        string GetDefaultDocumentCSS();
    }

    public class PageDeGardeTemplate
    {
        public string BackgroundGradient { get; set; } = "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
        public string TextColor { get; set; } = "white";
        public string FontFamily { get; set; } = "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";
        public bool ShowLogo { get; set; } = false;
        public string? LogoPath { get; set; }
    }

    public class TocTemplate
    {
        public string TitleColor { get; set; } = "#2c3e50";
        public string BorderColor { get; set; } = "#3498db";
        public string DotColor { get; set; } = "#ddd";
        public string PageNumberColor { get; set; } = "#3498db";
        public bool ShowDots { get; set; } = true;
    }

    public class SectionTemplate
    {
        public string TitleColor { get; set; } = "#2c3e50";
        public string SubtitleColor { get; set; } = "#2980b9";
        public string TextColor { get; set; } = "#333";
        public string BorderColor { get; set; } = "#3498db";
    }

    public class FTTemplate
    {
        public string HeaderBackgroundColor { get; set; } = "#f8f9fa";
        public string BorderColor { get; set; } = "#ddd";
        public bool ShowThumbnails { get; set; } = true;
        public string ThumbnailSize { get; set; } = "150px";
    }
}