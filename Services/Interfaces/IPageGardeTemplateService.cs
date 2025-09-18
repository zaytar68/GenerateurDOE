using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces
{
    public interface IPageGardeTemplateService
    {
        /// <summary>
        /// Obtient tous les templates de page de garde
        /// </summary>
        Task<IEnumerable<PageGardeTemplate>> GetAllTemplatesAsync();

        /// <summary>
        /// Obtient un template par son ID
        /// </summary>
        Task<PageGardeTemplate?> GetTemplateByIdAsync(int id);

        /// <summary>
        /// Obtient le template par défaut
        /// </summary>
        Task<PageGardeTemplate?> GetDefaultTemplateAsync();

        /// <summary>
        /// Créé un nouveau template
        /// </summary>
        Task<PageGardeTemplate> CreateTemplateAsync(PageGardeTemplate template);

        /// <summary>
        /// Met à jour un template existant
        /// </summary>
        Task<PageGardeTemplate> UpdateTemplateAsync(PageGardeTemplate template);

        /// <summary>
        /// Supprime un template
        /// </summary>
        Task<bool> DeleteTemplateAsync(int id);

        /// <summary>
        /// Définit un template comme défaut (et retire le défaut des autres)
        /// </summary>
        Task<bool> SetAsDefaultTemplateAsync(int id);

        /// <summary>
        /// Compile un template avec les données du document
        /// </summary>
        Task<string> CompileTemplateAsync(PageGardeTemplate template, DocumentGenere document, string typeDocument);

        /// <summary>
        /// Compile un template avec des données d'exemple pour la prévisualisation
        /// </summary>
        Task<string> CompileTemplateForPreviewAsync(PageGardeTemplate template);

        /// <summary>
        /// Obtient la liste des variables disponibles
        /// </summary>
        IEnumerable<TemplateVariable> GetAvailableVariables();
    }

    /// <summary>
    /// Représente une variable disponible dans les templates
    /// </summary>
    public class TemplateVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ExampleValue { get; set; } = string.Empty;
    }
}