using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service de gestion des méthodologies pour mémoires techniques avec images associées
/// Gère les méthodes de travail avec descriptions, images et ordre d'affichage
/// </summary>
public class MemoireTechniqueService : IMemoireTechniqueService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Initialise une nouvelle instance du service MemoireTechniqueService
    /// </summary>
    /// <param name="contextFactory">Factory pour créer les contextes EF thread-safe</param>
    /// <param name="appSettings">Configuration de l'application</param>
    public MemoireTechniqueService(IDbContextFactory<ApplicationDbContext> contextFactory, IOptions<AppSettings> appSettings)
    {
        _contextFactory = contextFactory;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// Récupère toutes les méthodes avec images triées par ordre d'affichage puis titre
    /// </summary>
    /// <returns>Méthodes avec images chargées et ordonnées</returns>
    public async Task<IEnumerable<Methode>> GetAllMethodesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.Methodes
            .Include(m => m.Images)
            .OrderBy(m => m.OrdreAffichage)
            .ThenBy(m => m.Titre)
            .ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Récupère une méthode par son identifiant avec ses images ordonnées
    /// </summary>
    /// <param name="id">Identifiant de la méthode</param>
    /// <returns>Méthode avec images triées par ordre d'affichage ou null</returns>
    public async Task<Methode?> GetMethodeByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.Methodes
            .Include(m => m.Images.OrderBy(i => i.OrdreAffichage))
            .FirstOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);
    }

    /// <summary>
    /// Crée une nouvelle méthode avec ordre d'affichage automatique et horodatage
    /// </summary>
    /// <param name="methode">Méthode à créer</param>
    /// <returns>Méthode créée avec ID généré et ordre calculé</returns>
    public async Task<Methode> CreateMethodeAsync(Methode methode)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        methode.DateCreation = DateTime.Now;
        methode.DateModification = DateTime.Now;

        if (methode.OrdreAffichage == 0)
        {
            var maxOrdre = await context.Methodes.MaxAsync(m => (int?)m.OrdreAffichage).ConfigureAwait(false) ?? 0;
            methode.OrdreAffichage = maxOrdre + 1;
        }

        context.Methodes.Add(methode);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return methode;
    }

    public async Task<Methode> UpdateMethodeAsync(Methode methode)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        methode.DateModification = DateTime.Now;

        context.Entry(methode).State = EntityState.Modified;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return methode;
    }

    public async Task<bool> DeleteMethodeAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var methode = await context.Methodes
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);

        if (methode == null)
            return false;

        foreach (var image in methode.Images)
        {
            if (File.Exists(image.CheminFichier))
            {
                File.Delete(image.CheminFichier);
            }
        }

        context.Methodes.Remove(methode);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<ImageMethode> AddImageToMethodeAsync(int methodeId, ImageMethode imageMethode)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        imageMethode.MethodeId = methodeId;
        imageMethode.DateImport = DateTime.Now;

        if (imageMethode.OrdreAffichage == 0)
        {
            var maxOrdre = await context.ImagesMethode
                .Where(i => i.MethodeId == methodeId)
                .MaxAsync(i => (int?)i.OrdreAffichage).ConfigureAwait(false) ?? 0;
            imageMethode.OrdreAffichage = maxOrdre + 1;
        }

        context.ImagesMethode.Add(imageMethode);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return imageMethode;
    }

    public async Task<bool> RemoveImageFromMethodeAsync(int imageId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var image = await context.ImagesMethode.FindAsync(imageId).ConfigureAwait(false);
        if (image == null)
            return false;

        if (File.Exists(image.CheminFichier))
        {
            File.Delete(image.CheminFichier);
        }

        context.ImagesMethode.Remove(image);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<string> SaveImageFileAsync(Stream fileStream, string originalFileName)
    {
        var repertoireStockage = _appSettings.RepertoireStockageImages;
        
        if (!Directory.Exists(repertoireStockage))
        {
            Directory.CreateDirectory(repertoireStockage);
        }

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}";
        var filePath = Path.Combine(repertoireStockage, fileName);

        using (var outputFileStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputFileStream);
        }

        return filePath;
    }

    public async Task<IEnumerable<Methode>> GetMethodesOrderedAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.Methodes
            .Include(m => m.Images.OrderBy(i => i.OrdreAffichage))
            .OrderBy(m => m.OrdreAffichage)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task UpdateMethodesOrderAsync(IEnumerable<(int id, int ordre)> ordres)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (var (id, ordre) in ordres)
        {
            var methode = await context.Methodes.FindAsync(id).ConfigureAwait(false);
            if (methode != null)
            {
                methode.OrdreAffichage = ordre;
                methode.DateModification = DateTime.Now;
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}