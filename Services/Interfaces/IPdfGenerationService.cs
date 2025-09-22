using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces
{
    public interface IPdfGenerationService
    {
        /// <summary>
        /// Génère un PDF complet en assemblant toutes les sections
        /// </summary>
        Task<byte[]> GenerateCompletePdfAsync(DocumentGenere document, PdfGenerationOptions? options = null);

        /// <summary>
        /// Convertit du contenu HTML en PDF via PuppeteerSharp
        /// </summary>
        Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, PdfGenerationOptions? options = null);

        /// <summary>
        /// Assemble plusieurs PDFs en un seul document
        /// </summary>
        Task<byte[]> AssemblePdfsAsync(IEnumerable<byte[]> pdfBytes, PdfAssemblyOptions? options = null);

        /// <summary>
        /// Génère une page de garde en PDF
        /// </summary>
        Task<byte[]> GeneratePageDeGardeAsync(DocumentGenere document, string typeDocument, PdfGenerationOptions? options = null);

        /// <summary>
        /// Génère une table des matières en PDF
        /// </summary>
        Task<byte[]> GenerateTableMatieresAsync(TableOfContentsData tocData, DocumentGenere document, PdfGenerationOptions? options = null);

        /// <summary>
        /// Optimise un PDF (compression, métadonnées, PDF/A)
        /// </summary>
        Task<byte[]> OptimizePdfAsync(byte[] pdfBytes, PdfOptimizationOptions? options = null);
    }

    public class PdfGenerationOptions
    {
        public string Format { get; set; } = "A4";
        public bool DisplayHeaderFooter { get; set; } = true;
        public string? HeaderTemplate { get; set; }
        public string? FooterTemplate { get; set; }
        public bool PrintBackground { get; set; } = true;
        public string MarginTop { get; set; } = "20mm";
        public string MarginBottom { get; set; } = "20mm";
        public string MarginLeft { get; set; } = "20mm";
        public string MarginRight { get; set; } = "20mm";
        public int Scale { get; set; } = 1;
        public int WaitForTimeout { get; set; } = 30000;
    }

    public class PdfAssemblyOptions
    {
        public bool AddBookmarks { get; set; } = true;
        public bool AddPageNumbers { get; set; } = true;
        public string PageNumberFormat { get; set; } = "Page {0} sur {1}";
        public bool OptimizeForPrint { get; set; } = true;
    }

    public class PdfOptimizationOptions
    {
        public bool CompressImages { get; set; } = true;
        public bool EmbedFonts { get; set; } = true;
        public bool CreatePdfA { get; set; } = false;
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Keywords { get; set; } = "";
    }

    public class TableOfContentsData
    {
        public List<TocEntry> Entries { get; set; } = new();
    }

    public class TocEntry
    {
        public string Title { get; set; } = "";
        public int Level { get; set; }
        public int PageNumber { get; set; }
        public List<TocEntry> Children { get; set; } = new();
    }
}