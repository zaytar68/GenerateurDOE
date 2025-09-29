using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionConteneursAndFTConteneurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionConteneursId",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnCours",
                table: "DocumentsGeneres",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "FTConteneurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    AfficherTableauRecapitulatif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DocumentGenereId = table.Column<int>(type: "int", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DocumentGenereId = table.Column<int>(type: "int", nullable: false),
                    TypeSectionId = table.Column<int>(type: "int", nullable: false)
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
                name: "FTElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionMarche = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroPage = table.Column<int>(type: "int", nullable: true),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FTConteneursId = table.Column<int>(type: "int", nullable: false),
                    FicheTechniqueId = table.Column<int>(type: "int", nullable: false),
                    ImportPDFId = table.Column<int>(type: "int", nullable: true)
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
                name: "IX_SectionsLibres_SectionConteneursId",
                table: "SectionsLibres",
                column: "SectionConteneursId");

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
                name: "IX_SectionConteneur_DocumentGenere_TypeSection",
                table: "SectionsConteneurs",
                columns: new[] { "DocumentGenereId", "TypeSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionsConteneurs_TypeSectionId",
                table: "SectionsConteneurs",
                column: "TypeSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneursId",
                table: "SectionsLibres",
                column: "SectionConteneursId",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneursId",
                table: "SectionsLibres");

            migrationBuilder.DropTable(
                name: "FTElements");

            migrationBuilder.DropTable(
                name: "SectionsConteneurs");

            migrationBuilder.DropTable(
                name: "FTConteneurs");

            migrationBuilder.DropIndex(
                name: "IX_SectionsLibres_SectionConteneursId",
                table: "SectionsLibres");

            migrationBuilder.DropColumn(
                name: "SectionConteneursId",
                table: "SectionsLibres");

            migrationBuilder.DropColumn(
                name: "EnCours",
                table: "DocumentsGeneres");
        }
    }
}
