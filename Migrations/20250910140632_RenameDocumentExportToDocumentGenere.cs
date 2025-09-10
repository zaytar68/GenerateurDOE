using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class RenameDocumentExportToDocumentGenere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentsExport");

            migrationBuilder.CreateTable(
                name: "DocumentsGeneres",
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
                    table.PrimaryKey("PK_DocumentsGeneres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentsGeneres_Chantiers_ChantierId",
                        column: x => x.ChantierId,
                        principalTable: "Chantiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentGenereFicheTechniques",
                columns: table => new
                {
                    DocumentGenereId = table.Column<int>(type: "int", nullable: false),
                    FicheTechniqueId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DocumentGenereFicheTechniques_FicheTechniqueId",
                table: "DocumentGenereFicheTechniques",
                column: "FicheTechniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsGeneres_ChantierId",
                table: "DocumentsGeneres",
                column: "ChantierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentGenereFicheTechniques");

            migrationBuilder.DropTable(
                name: "DocumentsGeneres");

            migrationBuilder.CreateTable(
                name: "DocumentsExport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChantierId = table.Column<int>(type: "int", nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FormatExport = table.Column<int>(type: "int", nullable: false),
                    IncludePageDeGarde = table.Column<bool>(type: "bit", nullable: false),
                    IncludeTableMatieres = table.Column<bool>(type: "bit", nullable: false),
                    NomFichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Parametres = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TypeDocument = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsExport_ChantierId",
                table: "DocumentsExport",
                column: "ChantierId");
        }
    }
}
