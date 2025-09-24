using System.Text.Json.Serialization;

namespace GenerateurDOE.Models
{
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
    /// Classe partagée entre les composants Blazor et le service PDF
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
    /// Structure de données pour la table des matières
    /// </summary>
    public class TableOfContentsData
    {
        public List<TocEntry> Entries { get; set; } = new List<TocEntry>();
    }

    /// <summary>
    /// Entrée de table des matières pour la génération PDF
    /// </summary>
    public class TocEntry
    {
        public string Title { get; set; } = "";
        public int Level { get; set; } = 1;
        public int PageNumber { get; set; } = 1;
        public List<TocEntry> Children { get; set; } = new List<TocEntry>();
    }
}