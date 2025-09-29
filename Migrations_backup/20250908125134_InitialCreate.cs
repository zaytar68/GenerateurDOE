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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomProjet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaitreOeuvre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaitreOuvrage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NumeroLot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IntituleLot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chantiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Methodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Methodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentsExport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeDocument = table.Column<int>(type: "int", nullable: false),
                    FormatExport = table.Column<int>(type: "int", nullable: false),
                    NomFichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Parametres = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IncludePageDeGarde = table.Column<bool>(type: "bit", nullable: false),
                    IncludeTableMatieres = table.Column<bool>(type: "bit", nullable: false),
                    ChantierId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentsExport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsExport_Chantiers_ChantierId",
                        column: x => x.ChantierId,
                        principalTable: "Chantiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichesTechniques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomProduit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomFabricant = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TypeProduit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ChantierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichesTechniques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichesTechniques_Chantiers_ChantierId",
                        column: x => x.ChantierId,
                        principalTable: "Chantiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImagesMethode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    DateImport = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    MethodeId = table.Column<int>(type: "int", nullable: false)
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
                name: "ImportsPDF",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TypeDocument = table.Column<int>(type: "int", nullable: false),
                    TailleFichier = table.Column<long>(type: "bigint", nullable: false),
                    DateImport = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FicheTechniqueId = table.Column<int>(type: "int", nullable: false)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsExport_ChantierId",
                table: "DocumentsExport",
                column: "ChantierId");

            migrationBuilder.CreateIndex(
                name: "IX_FichesTechniques_ChantierId",
                table: "FichesTechniques",
                column: "ChantierId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesMethode_MethodeId",
                table: "ImagesMethode",
                column: "MethodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportsPDF_FicheTechniqueId",
                table: "ImportsPDF",
                column: "FicheTechniqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentsExport");

            migrationBuilder.DropTable(
                name: "ImagesMethode");

            migrationBuilder.DropTable(
                name: "ImportsPDF");

            migrationBuilder.DropTable(
                name: "Methodes");

            migrationBuilder.DropTable(
                name: "FichesTechniques");

            migrationBuilder.DropTable(
                name: "Chantiers");
        }
    }
}
