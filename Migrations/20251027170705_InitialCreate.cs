using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chantiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomProjet = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaitreOeuvre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaitreOuvrage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EstArchive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chantiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Methodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    OrdreAffichage = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Methodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageGardeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContenuHtml = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    ContenuJson = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    EstParDefaut = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageGardeTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesProduits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesProduits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImagesMethode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheminFichier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OrdreAffichage = table.Column<int>(type: "INTEGER", nullable: false),
                    DateImport = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    MethodeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagesMethode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagesMethode_Methodes_MethodeId",
                        column: x => x.MethodeId,
                        principalTable: "Methodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentsGeneres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeDocument = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatExport = table.Column<int>(type: "INTEGER", nullable: false),
                    NomFichier = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CheminFichier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Parametres = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IncludePageDeGarde = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeTableMatieres = table.Column<bool>(type: "INTEGER", nullable: false),
                    PageGardeTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    EnCours = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NumeroLot = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IntituleLot = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ChantierId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsGeneres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsGeneres_Chantiers_ChantierId",
                        column: x => x.ChantierId,
                        principalTable: "Chantiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentsGeneres_PageGardeTemplates_PageGardeTemplateId",
                        column: x => x.PageGardeTemplateId,
                        principalTable: "PageGardeTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FichesTechniques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomProduit = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NomFabricant = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeProduit = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ChantierId = table.Column<int>(type: "INTEGER", nullable: true),
                    TypeProduitId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichesTechniques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichesTechniques_Chantiers_ChantierId",
                        column: x => x.ChantierId,
                        principalTable: "Chantiers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FichesTechniques_TypesProduits_TypeProduitId",
                        column: x => x.TypeProduitId,
                        principalTable: "TypesProduits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SectionsLibres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    ContenuHtml = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    ContenuJson = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TypeSectionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionsLibres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionsLibres_TypesSections_TypeSectionId",
                        column: x => x.TypeSectionId,
                        principalTable: "TypesSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FTConteneurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    AfficherTableauRecapitulatif = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DocumentGenereId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTConteneurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FTConteneurs_DocumentsGeneres_DocumentGenereId",
                        column: x => x.DocumentGenereId,
                        principalTable: "DocumentsGeneres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionsConteneurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    Titre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DocumentGenereId = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeSectionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionsConteneurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionsConteneurs_DocumentsGeneres_DocumentGenereId",
                        column: x => x.DocumentGenereId,
                        principalTable: "DocumentsGeneres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionsConteneurs_TypesSections_TypeSectionId",
                        column: x => x.TypeSectionId,
                        principalTable: "TypesSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentGenereFicheTechniques",
                columns: table => new
                {
                    DocumentGenereId = table.Column<int>(type: "INTEGER", nullable: false),
                    FicheTechniqueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentGenereFicheTechniques", x => new { x.DocumentGenereId, x.FicheTechniqueId });
                    table.ForeignKey(
                        name: "FK_DocumentGenereFicheTechniques_DocumentsGeneres_DocumentGenereId",
                        column: x => x.DocumentGenereId,
                        principalTable: "DocumentsGeneres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentGenereFicheTechniques_FichesTechniques_FicheTechniqueId",
                        column: x => x.FicheTechniqueId,
                        principalTable: "FichesTechniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportsPDF",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheminFichier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TypeDocumentImportId = table.Column<int>(type: "INTEGER", nullable: false),
                    TailleFichier = table.Column<long>(type: "INTEGER", nullable: false),
                    DateImport = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    FicheTechniqueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportsPDF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportsPDF_FichesTechniques_FicheTechniqueId",
                        column: x => x.FicheTechniqueId,
                        principalTable: "FichesTechniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportsPDF_TypesDocuments_TypeDocumentImportId",
                        column: x => x.TypeDocumentImportId,
                        principalTable: "TypesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SectionConteneurItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SectionConteneursId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionLibreId = table.Column<int>(type: "INTEGER", nullable: false),
                    TitrePersonnalise = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContenuHtmlPersonnalise = table.Column<string>(type: "TEXT", maxLength: 2147483647, nullable: true),
                    DateModificationPersonnalisation = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionConteneurItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionConteneurItems_SectionsConteneurs_SectionConteneursId",
                        column: x => x.SectionConteneursId,
                        principalTable: "SectionsConteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionConteneurItems_SectionsLibres_SectionLibreId",
                        column: x => x.SectionLibreId,
                        principalTable: "SectionsLibres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FTElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PositionMarche = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroPage = table.Column<int>(type: "INTEGER", nullable: true),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    Commentaire = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FTConteneursId = table.Column<int>(type: "INTEGER", nullable: false),
                    FicheTechniqueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportPDFId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FTElements_FTConteneurs_FTConteneursId",
                        column: x => x.FTConteneursId,
                        principalTable: "FTConteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FTElements_FichesTechniques_FicheTechniqueId",
                        column: x => x.FicheTechniqueId,
                        principalTable: "FichesTechniques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FTElements_ImportsPDF_ImportPDFId",
                        column: x => x.ImportPDFId,
                        principalTable: "ImportsPDF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentGenereFicheTechniques_FicheTechniqueId",
                table: "DocumentGenereFicheTechniques",
                column: "FicheTechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsGeneres_ChantierId",
                table: "DocumentsGeneres",
                column: "ChantierId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsGeneres_PageGardeTemplateId",
                table: "DocumentsGeneres",
                column: "PageGardeTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FichesTechniques_ChantierId",
                table: "FichesTechniques",
                column: "ChantierId");

            migrationBuilder.CreateIndex(
                name: "IX_FichesTechniques_TypeProduitId",
                table: "FichesTechniques",
                column: "TypeProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_FTConteneurs_DocumentGenereId",
                table: "FTConteneurs",
                column: "DocumentGenereId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FTElements_FicheTechniqueId",
                table: "FTElements",
                column: "FicheTechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_FTElements_FTConteneursId",
                table: "FTElements",
                column: "FTConteneursId");

            migrationBuilder.CreateIndex(
                name: "IX_FTElements_ImportPDFId",
                table: "FTElements",
                column: "ImportPDFId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesMethode_MethodeId",
                table: "ImagesMethode",
                column: "MethodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportsPDF_FicheTechniqueId",
                table: "ImportsPDF",
                column: "FicheTechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportsPDF_TypeDocumentImportId",
                table: "ImportsPDF",
                column: "TypeDocumentImportId");

            migrationBuilder.CreateIndex(
                name: "IX_PageGardeTemplates_Nom",
                table: "PageGardeTemplates",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionConteneurItems_SectionConteneursId_SectionLibreId",
                table: "SectionConteneurItems",
                columns: new[] { "SectionConteneursId", "SectionLibreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionConteneurItems_SectionLibreId",
                table: "SectionConteneurItems",
                column: "SectionLibreId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionConteneur_DocumentGenere_TypeSection",
                table: "SectionsConteneurs",
                columns: new[] { "DocumentGenereId", "TypeSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionsConteneurs_TypeSectionId",
                table: "SectionsConteneurs",
                column: "TypeSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_TypeSectionId",
                table: "SectionsLibres",
                column: "TypeSectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentGenereFicheTechniques");

            migrationBuilder.DropTable(
                name: "FTElements");

            migrationBuilder.DropTable(
                name: "ImagesMethode");

            migrationBuilder.DropTable(
                name: "SectionConteneurItems");

            migrationBuilder.DropTable(
                name: "FTConteneurs");

            migrationBuilder.DropTable(
                name: "ImportsPDF");

            migrationBuilder.DropTable(
                name: "Methodes");

            migrationBuilder.DropTable(
                name: "SectionsConteneurs");

            migrationBuilder.DropTable(
                name: "SectionsLibres");

            migrationBuilder.DropTable(
                name: "FichesTechniques");

            migrationBuilder.DropTable(
                name: "TypesDocuments");

            migrationBuilder.DropTable(
                name: "DocumentsGeneres");

            migrationBuilder.DropTable(
                name: "TypesSections");

            migrationBuilder.DropTable(
                name: "TypesProduits");

            migrationBuilder.DropTable(
                name: "Chantiers");

            migrationBuilder.DropTable(
                name: "PageGardeTemplates");
        }
    }
}
