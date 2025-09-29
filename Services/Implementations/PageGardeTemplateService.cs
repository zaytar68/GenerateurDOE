using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Services.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace GenerateurDOE.Services.Implementations
{
    public class PageGardeTemplateService : IPageGardeTemplateService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILoggingService _loggingService;
        private readonly IConfigurationService _configurationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PageGardeTemplateService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILoggingService loggingService,
            IConfigurationService configurationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _loggingService = loggingService;
            _configurationService = configurationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<PageGardeTemplate>> GetAllTemplatesAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.PageGardeTemplates
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
                using var context = _contextFactory.CreateDbContext();
                return await context.PageGardeTemplates.FindAsync(id);
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
                using var context = _contextFactory.CreateDbContext();
                return await context.PageGardeTemplates
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
                using var context = _contextFactory.CreateDbContext();
                template.DateCreation = DateTime.Now;
                template.DateModification = DateTime.Now;

                // Si c'est le premier template, le marquer comme défaut
                if (!await context.PageGardeTemplates.AnyAsync())
                {
                    template.EstParDefaut = true;
                }

                context.PageGardeTemplates.Add(template);
                await context.SaveChangesAsync();

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
                using var context = _contextFactory.CreateDbContext();
                var existingTemplate = await context.PageGardeTemplates.FindAsync(template.Id);
                if (existingTemplate == null)
                {
                    throw new ArgumentException($"Template avec ID {template.Id} non trouvé");
                }

                existingTemplate.Nom = template.Nom;
                existingTemplate.Description = template.Description;
                existingTemplate.ContenuHtml = template.ContenuHtml;
                existingTemplate.ContenuJson = template.ContenuJson;
                existingTemplate.DateModification = DateTime.Now;

                await context.SaveChangesAsync();

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
                using var context = _contextFactory.CreateDbContext();
                var template = await context.PageGardeTemplates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                // Vérifier si c'est le template par défaut
                if (template.EstParDefaut)
                {
                    var otherTemplate = await context.PageGardeTemplates
                        .Where(t => t.Id != id)
                        .FirstOrDefaultAsync();

                    if (otherTemplate != null)
                    {
                        otherTemplate.EstParDefaut = true;
                    }
                }

                context.PageGardeTemplates.Remove(template);
                await context.SaveChangesAsync();

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
                using var context = _contextFactory.CreateDbContext();
                var template = await context.PageGardeTemplates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                // Retirer le défaut de tous les autres templates
                var currentDefault = await context.PageGardeTemplates
                    .Where(t => t.EstParDefaut && t.Id != id)
                    .ToListAsync();

                foreach (var t in currentDefault)
                {
                    t.EstParDefaut = false;
                }

                // Définir le nouveau défaut
                template.EstParDefaut = true;
                await context.SaveChangesAsync();

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
                html = Regex.Replace(html, @"\{\{document\.numeroLot\}\}", document.NumeroLot ?? "", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{document\.intituleLot\}\}", document.IntituleLot ?? "", RegexOptions.IgnoreCase);

                // Remplacer les variables du chantier
                if (document.Chantier != null)
                {
                    html = Regex.Replace(html, @"\{\{chantier\.nomProjet\}\}", document.Chantier.NomProjet ?? "", RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.maitreOeuvre\}\}", document.Chantier.MaitreOeuvre ?? "", RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.maitreOuvrage\}\}", document.Chantier.MaitreOuvrage ?? "", RegexOptions.IgnoreCase);
                    html = Regex.Replace(html, @"\{\{chantier\.adresse\}\}", document.Chantier.Adresse ?? "", RegexOptions.IgnoreCase);
                }

                // Remplacer les variables système
                html = Regex.Replace(html, @"\{\{system\.date\}\}", DateTime.Now.ToString("dd/MM/yyyy"), RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"\{\{system\.nomEntreprise\}\}", "Réalisé par notre société", RegexOptions.IgnoreCase);

                // Remplacer la variable logo avec URL absolue (même méthode que les sections libres)
                var logoUrl = await GetLogoUrlAsync();
                html = Regex.Replace(html, @"\{\{system\.logo\}\}", logoUrl, RegexOptions.IgnoreCase);

                // Convertir les URLs relatives des images en URLs absolues (même logique que les sections libres)
                html = ConvertRelativeImagesToAbsolute(html);

                // Encapsuler dans une structure HTML complète avec les styles corrigés
                html = WrapWithPageStructure(html);

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

                // Remplacer la variable logo avec URL absolue (même méthode que les sections libres)
                var logoUrl = await GetLogoUrlAsync();
                html = Regex.Replace(html, @"\{\{system\.logo\}\}", logoUrl, RegexOptions.IgnoreCase);

                // Convertir les URLs relatives des images en URLs absolues (même logique que les sections libres)
                html = ConvertRelativeImagesToAbsolute(html);

                // Encapsuler dans une structure HTML complète avec les styles corrigés
                html = WrapWithPageStructure(html);

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
                new TemplateVariable { Name = "Nom de l'entreprise", Placeholder = "{{system.nomEntreprise}}", Description = "Nom de votre entreprise", Category = "Système", ExampleValue = "Notre Entreprise SARL" },
                new TemplateVariable { Name = "Logo de l'entreprise", Placeholder = "{{system.logo}}", Description = "URL du logo de l'entreprise (automatiquement trouvé)", Category = "Système", ExampleValue = "http://localhost:5283/api/images/logo.png" }
            };
        }

        private async Task<string> GetLogoUrlAsync()
        {
            try
            {
                // Utiliser le répertoire d'images configuré
                var appSettings = await _configurationService.GetAppSettingsAsync();
                var imagesDirectory = appSettings.RepertoireStockageImages;

                _loggingService.LogInformation($"Recherche de logo dans le répertoire configuré : {imagesDirectory}");

                if (Directory.Exists(imagesDirectory))
                {
                    // Chercher d'abord un fichier "logo" ou "titre" (plus récent en premier)
                    var logoPatterns = new[] { "*logo*", "*titre*", "*Titre*", "*illustration*" };
                    var imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };

                    foreach (var pattern in logoPatterns)
                    {
                        foreach (var extension in imageExtensions)
                        {
                            var searchPattern = pattern + Path.GetExtension(extension);
                            var logoFiles = Directory.GetFiles(imagesDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                                                   .OrderByDescending(f => File.GetLastWriteTime(f))
                                                   .ToArray();

                            if (logoFiles.Any())
                            {
                                var logoPath = logoFiles.First();
                                var fileName = Path.GetFileName(logoPath);
                                _loggingService.LogInformation($"Logo trouvé pour template : {fileName}");

                                // Construire l'URL absolue vers l'API d'images (même méthode que les sections libres)
                                var logoUrl = $"{GetBaseUrl()}/api/images/{fileName}";
                                _loggingService.LogInformation($"URL logo générée pour template : {logoUrl}");
                                return logoUrl;
                            }
                        }
                    }

                    _loggingService.LogWarning($"Aucun fichier logo trouvé dans {imagesDirectory} pour le template");
                }
                else
                {
                    _loggingService.LogWarning($"Répertoire d'images non trouvé : {imagesDirectory}");
                }

                // Fallback vers une chaîne vide si aucun logo n'est trouvé
                _loggingService.LogWarning("Aucun logo disponible pour le template");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la recherche du logo pour template : {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Convertit les URLs relatives des images en URLs absolues dans le contenu HTML
        /// Transforme src="/images/nom.jpg" en src="http://localhost:5282/images/nom.jpg"
        /// </summary>
        /// <param name="htmlContent">Contenu HTML contenant potentiellement des images</param>
        /// <returns>HTML avec URLs d'images converties en absolues</returns>
        private string ConvertRelativeImagesToAbsolute(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return htmlContent;

            var baseUrl = GetBaseUrl();
            var pattern = @"src\s*=\s*[""']/images/([^""']+)[""']";
            var replacement = $"src=\"{baseUrl}/images/$1\"";

            var convertedHtml = Regex.Replace(htmlContent, pattern, replacement, RegexOptions.IgnoreCase);

            // Log pour diagnostic
            var matches = Regex.Matches(htmlContent, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                _loggingService.LogInformation($"Template : Conversion de {matches.Count} URLs d'images relatives vers URLs absolues avec base {baseUrl}");
                foreach (Match match in matches)
                {
                    _loggingService.LogInformation($"  Template image convertie : /images/{match.Groups[1].Value} → {baseUrl}/images/{match.Groups[1].Value}");
                }
            }

            return convertedHtml;
        }

        /// <summary>
        /// Obtient l'URL de base dynamique du serveur en cours d'exécution
        /// Utilise le contexte HTTP pour détecter automatiquement le scheme, host et port
        /// </summary>
        /// <returns>URL de base complète (ex: http://localhost:5282)</returns>
        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                _loggingService.LogInformation($"URL de base détectée dynamiquement pour template : {baseUrl}");
                return baseUrl;
            }

            // Fallback si pas de contexte HTTP disponible
            var fallbackUrl = "http://localhost:5282";
            _loggingService.LogWarning($"Aucun contexte HTTP disponible pour template, utilisation du fallback : {fallbackUrl}");
            return fallbackUrl;
        }

        private string WrapWithPageStructure(string bodyContent)
        {
            return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <base href=""http://localhost:5282/"">
    <title>Page de Garde</title>
    <style>
        {CssStylesHelper.GetCoverPageCSS()}
    </style>
</head>
<body>
    {bodyContent}
</body>
</html>";
        }
    }
}