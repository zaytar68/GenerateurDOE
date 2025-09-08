using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class MemoireTechniqueService : IMemoireTechniqueService
{
    private readonly ApplicationDbContext _context;
    private readonly AppSettings _appSettings;

    public MemoireTechniqueService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
    {
        _context = context;
        _appSettings = appSettings.Value;
    }

    public async Task<IEnumerable<Methode>> GetAllMethodesAsync()
    {
        return await _context.Methodes
            .Include(m => m.Images)
            .OrderBy(m => m.OrdreAffichage)
            .ThenBy(m => m.Titre)
            .ToListAsync();
    }

    public async Task<Methode?> GetMethodeByIdAsync(int id)
    {
        return await _context.Methodes
            .Include(m => m.Images.OrderBy(i => i.OrdreAffichage))
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Methode> CreateMethodeAsync(Methode methode)
    {
        methode.DateCreation = DateTime.Now;
        methode.DateModification = DateTime.Now;
        
        if (methode.OrdreAffichage == 0)
        {
            var maxOrdre = await _context.Methodes.MaxAsync(m => (int?)m.OrdreAffichage) ?? 0;
            methode.OrdreAffichage = maxOrdre + 1;
        }

        _context.Methodes.Add(methode);
        await _context.SaveChangesAsync();
        return methode;
    }

    public async Task<Methode> UpdateMethodeAsync(Methode methode)
    {
        methode.DateModification = DateTime.Now;

        _context.Entry(methode).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return methode;
    }

    public async Task<bool> DeleteMethodeAsync(int id)
    {
        var methode = await _context.Methodes
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (methode == null)
            return false;

        foreach (var image in methode.Images)
        {
            if (File.Exists(image.CheminFichier))
            {
                File.Delete(image.CheminFichier);
            }
        }

        _context.Methodes.Remove(methode);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ImageMethode> AddImageToMethodeAsync(int methodeId, ImageMethode imageMethode)
    {
        imageMethode.MethodeId = methodeId;
        imageMethode.DateImport = DateTime.Now;
        
        if (imageMethode.OrdreAffichage == 0)
        {
            var maxOrdre = await _context.ImagesMethode
                .Where(i => i.MethodeId == methodeId)
                .MaxAsync(i => (int?)i.OrdreAffichage) ?? 0;
            imageMethode.OrdreAffichage = maxOrdre + 1;
        }

        _context.ImagesMethode.Add(imageMethode);
        await _context.SaveChangesAsync();
        return imageMethode;
    }

    public async Task<bool> RemoveImageFromMethodeAsync(int imageId)
    {
        var image = await _context.ImagesMethode.FindAsync(imageId);
        if (image == null)
            return false;

        if (File.Exists(image.CheminFichier))
        {
            File.Delete(image.CheminFichier);
        }

        _context.ImagesMethode.Remove(image);
        await _context.SaveChangesAsync();
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
        return await _context.Methodes
            .Include(m => m.Images.OrderBy(i => i.OrdreAffichage))
            .OrderBy(m => m.OrdreAffichage)
            .ToListAsync();
    }

    public async Task UpdateMethodesOrderAsync(IEnumerable<(int id, int ordre)> ordres)
    {
        foreach (var (id, ordre) in ordres)
        {
            var methode = await _context.Methodes.FindAsync(id);
            if (methode != null)
            {
                methode.OrdreAffichage = ordre;
                methode.DateModification = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
    }
}