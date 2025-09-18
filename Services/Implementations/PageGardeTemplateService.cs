using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GenerateurDOE.Services.Implementations
{
    public class PageGardeTemplateService : IPageGardeTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoggingService _loggingService;

        public PageGardeTemplateService(ApplicationDbContext context, ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        public async Task<IEnumerable<PageGardeTemplate>> GetAllTemplatesAsync()
        {
            try
            {
                return await _context.PageGardeTemplates
                    .OrderByDescending(t => t.EstParDefaut)
                    .ThenBy(t => t.Nom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Erreur lors de la récupération des templates de page de garde");
                throw;
            }
        }

        public async Task<PageGardeTemplate?> GetTemplateByIdAsync(int id)
        {
            try
            {
                return await _context.PageGardeTemplates.FindAsync(id);
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la récupération du template ID {id}");
                throw;
            }
        }

        public async Task<PageGardeTemplate?> GetDefaultTemplateAsync()
        {
            try
            {
                return await _context.PageGardeTemplates
                    .FirstOrDefaultAsync(t => t.EstParDefaut);
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Erreur lors de la récupération du template par défaut");
                throw;
            }
        }

        public async Task<PageGardeTemplate> CreateTemplateAsync(PageGardeTemplate template)
        {
            try
            {
                template.DateCreation = DateTime.Now;
                template.DateModification = DateTime.Now;

                // Si c'est le premier template, le marquer comme défaut
                if (!await _context.PageGardeTemplates.AnyAsync())
                {
                    template.EstParDefaut = true;
                }

                _context.PageGardeTemplates.Add(template);
                await _context.SaveChangesAsync();

_loggingService.LogInformation($"Template de page de garde créé : {template.Nom}");
                return template;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la création du template {template.Nom}");
                throw;
            }
        }

        public async Task<PageGardeTemplate> UpdateTemplateAsync(PageGardeTemplate template)
        {
            try
            {
                var existingTemplate = await _context.PageGardeTemplates.FindAsync(template.Id);
                if (existingTemplate == null)
                {
                    throw new ArgumentException($"Template avec ID {template.Id} non trouvé");
                }

                existingTemplate.Nom = template.Nom;
                existingTemplate.Description = template.Description;
                existingTemplate.ContenuHtml = template.ContenuHtml;
                existingTemplate.ContenuJson = template.ContenuJson;
                existingTemplate.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();

_loggingService.LogInformation($"Template de page de garde mis à jour : {template.Nom}");
                return existingTemplate;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la mise à jour du template {template.Nom}");
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            try
            {
                var template = await _context.PageGardeTemplates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                // Vérifier si c'est le template par défaut
                if (template.EstParDefaut)
                {
                    var otherTemplate = await _context.PageGardeTemplates
                        .Where(t => t.Id != id)
                        .FirstOrDefaultAsync();

                    if (otherTemplate != null)
                    {
                        otherTemplate.EstParDefaut = true;
                    }
                }

                _context.PageGardeTemplates.Remove(template);
                await _context.SaveChangesAsync();

_loggingService.LogInformation($"Template de page de garde supprimé : {template.Nom}");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la suppression du template ID {id}");
                throw;
            }
        }

        public async Task<bool> SetAsDefaultTemplateAsync(int id)
        {
            try
            {
                var template = await _context.PageGardeTemplates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                // Retirer le défaut de tous les autres templates
                var currentDefault = await _context.PageGardeTemplates
                    .Where(t => t.EstParDefaut && t.Id != id)
                    .ToListAsync();

                foreach (var t in currentDefault)
                {
                    t.EstParDefaut = false;
                }

                // Définir le nouveau défaut
                template.EstParDefaut = true;
                await _context.SaveChangesAsync();

_loggingService.LogInformation($"Template défini comme défaut : {template.Nom}");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la définition du template par défaut ID {id}");
                throw;
            }
        }

        public async Task<string> CompileTemplateAsync(PageGardeTemplate template, DocumentGenere document, string typeDocument)
        {
            try
            {
                var html = template.ContenuHtml;

                // Remplacer les variables du document
                html = Regex.Replace(html, @"\{\{document\.type\}\}", typeDocument, RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{document\.numeroLot\}\}", document.NumeroLot, RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{document\.intituleLot\}\}", document.IntituleLot, RegexOptions.IgnoreCase);

                // Remplacer les variables du chantier
                if (document.Chantier != null)
                {
                    html = Regex.Replace(html, @"\{\{chantier\.nomProjet\}\}", document.Chantier.NomProjet, RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.maitreOeuvre\}\}", document.Chantier.MaitreOeuvre, RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.maitreOuvrage\}\}", document.Chantier.MaitreOuvrage, RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.adresse\}\}", document.Chantier.Adresse, RegexOptions.IgnoreCase);
                }

                // Remplacer les variables système
                html = Regex.Replace(html, @"\{\{system\.date\}\}", DateTime.Now.ToString("dd/MM/yyyy"), RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{system\.nomEntreprise\}\}", "Réalisé par notre société", RegexOptions.IgnoreCase);

                await Task.CompletedTask;
                return html;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la compilation du template {template.Nom}");
                throw;
            }
        }

        public async Task<string> CompileTemplateForPreviewAsync(PageGardeTemplate template)
        {
            try
            {
                var html = template.ContenuHtml;

                // Remplacer avec des données d'exemple
                html = Regex.Replace(html, @"\{\{document\.type\}\}", "DOE - Exemple", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{document\.numeroLot\}\}", "02", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{document\.intituleLot\}\}", "Électricité générale", RegexOptions.IgnoreCase);

                html = Regex.Replace(html, @"\{\{chantier\.nomProjet\}\}", "Résidence Les Jardins", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{chantier\.maitreOeuvre\}\}", "Bureau d'études ABC", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{chantier\.maitreOuvrage\}\}", "SCI Les Jardins", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{chantier\.adresse\}\}", "123 Avenue de la République, 75000 Paris", RegexOptions.IgnoreCase);

                html = Regex.Replace(html, @"\{\{system\.date\}\}", DateTime.Now.ToString("dd/MM/yyyy"), RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{system\.nomEntreprise\}\}", "Notre Entreprise SARL", RegexOptions.IgnoreCase);

                await Task.CompletedTask;
                return html;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la compilation preview du template {template.Nom}");
                throw;
            }
        }

        public IEnumerable<TemplateVariable> GetAvailableVariables()
        {
            return new List<TemplateVariable>
            {
                // Variables du document
                new TemplateVariable { Name = "Type de document", Placeholder = "{{document.type}}", Description = "Type du document (DOE, Dossier technique, etc.)", Category = "Document", ExampleValue = "DOE" },
                new TemplateVariable { Name = "Numéro de lot", Placeholder = "{{document.numeroLot}}", Description = "Numéro du lot", Category = "Document", ExampleValue = "02" },
                new TemplateVariable { Name = "Intitulé du lot", Placeholder = "{{document.intituleLot}}", Description = "Intitulé complet du lot", Category = "Document", ExampleValue = "Électricité générale" },

                // Variables du chantier
                new TemplateVariable { Name = "Nom du projet", Placeholder = "{{chantier.nomProjet}}", Description = "Nom du projet/chantier", Category = "Chantier", ExampleValue = "Résidence Les Jardins" },
                new TemplateVariable { Name = "Maître d'œuvre", Placeholder = "{{chantier.maitreOeuvre}}", Description = "Nom du maître d'œuvre", Category = "Chantier", ExampleValue = "Bureau d'études ABC" },
                new TemplateVariable { Name = "Maître d'ouvrage", Placeholder = "{{chantier.maitreOuvrage}}", Description = "Nom du maître d'ouvrage", Category = "Chantier", ExampleValue = "SCI Les Jardins" },
                new TemplateVariable { Name = "Adresse", Placeholder = "{{chantier.adresse}}", Description = "Adresse complète du chantier", Category = "Chantier", ExampleValue = "123 Avenue de la République, 75000 Paris" },

                // Variables système
                new TemplateVariable { Name = "Date actuelle", Placeholder = "{{system.date}}", Description = "Date de génération du document", Category = "Système", ExampleValue = DateTime.Now.ToString("dd/MM/yyyy") },
                new TemplateVariable { Name = "Nom de l'entreprise", Placeholder = "{{system.nomEntreprise}}", Description = "Nom de votre entreprise", Category = "Système", ExampleValue = "Notre Entreprise SARL" }
            };
        }
    }
}