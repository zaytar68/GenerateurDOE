using Microsoft.EntityFrameworkCore;
using GenerateurDOE.Models;

namespace GenerateurDOE.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chantier> Chantiers { get; set; }
    public DbSet<FicheTechnique> FichesTechniques { get; set; }
    public DbSet<ImportPDF> ImportsPDF { get; set; }
    public DbSet<Methode> Methodes { get; set; }
    public DbSet<ImageMethode> ImagesMethode { get; set; }
    public DbSet<DocumentGenere> DocumentsGeneres { get; set; }
    public DbSet<TypeProduit> TypesProduits { get; set; }
    public DbSet<TypeDocumentImport> TypesDocuments { get; set; }
    public DbSet<TypeSection> TypesSections { get; set; }
    public DbSet<SectionLibre> SectionsLibres { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chantier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomProjet).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MaitreOeuvre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MaitreOuvrage).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Adresse).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NumeroLot).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IntituleLot).HasMaxLength(300).IsRequired();
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<FicheTechnique>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomProduit).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NomFabricant).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TypeProduit).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
                
        });

        modelBuilder.Entity<ImportPDF>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CheminFichier).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NomFichierOriginal).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TypeDocument).IsRequired();
            entity.Property(e => e.DateImport).HasDefaultValueSql("GETDATE()");
            
            entity.HasOne(d => d.FicheTechnique)
                .WithMany(p => p.ImportsPDF)
                .HasForeignKey(d => d.FicheTechniqueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Methode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(5000).IsRequired();
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<ImageMethode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CheminFichier).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NomFichierOriginal).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DateImport).HasDefaultValueSql("GETDATE()");
            
            entity.HasOne(d => d.Methode)
                .WithMany(p => p.Images)
                .HasForeignKey(d => d.MethodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentGenere>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypeDocument).IsRequired();
            entity.Property(e => e.FormatExport).IsRequired();
            entity.Property(e => e.NomFichier).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CheminFichier).HasMaxLength(500);
            entity.Property(e => e.Parametres).HasMaxLength(2000);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            
            entity.HasOne(d => d.Chantier)
                .WithMany(p => p.DocumentsGeneres)
                .HasForeignKey(d => d.ChantierId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(d => d.FichesTechniques)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "DocumentGenereFicheTechnique",
                    j => j.HasOne<FicheTechnique>().WithMany().HasForeignKey("FicheTechniqueId"),
                    j => j.HasOne<DocumentGenere>().WithMany().HasForeignKey("DocumentGenereId"),
                    j =>
                    {
                        j.HasKey("DocumentGenereId", "FicheTechniqueId");
                        j.ToTable("DocumentGenereFicheTechniques");
                    });
        });

        modelBuilder.Entity<TypeProduit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<TypeDocumentImport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<TypeSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<SectionLibre>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Ordre).IsRequired();
            entity.Property(e => e.ContenuHtml).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ContenuJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DateCreation).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.DateModification).HasDefaultValueSql("GETDATE()");
            
            entity.HasOne(d => d.TypeSection)
                .WithMany(p => p.SectionsLibres)
                .HasForeignKey(d => d.TypeSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}