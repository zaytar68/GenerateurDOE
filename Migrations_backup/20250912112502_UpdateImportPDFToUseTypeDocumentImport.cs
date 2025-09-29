using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateImportPDFToUseTypeDocumentImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectionLibreSectionConteneurs");

            // Ajouter une colonne temporaire pour la migration
            migrationBuilder.AddColumn<int>(
                name: "TypeDocumentImportId",
                table: "ImportsPDF",
                type: "int",
                nullable: false,
                defaultValue: 1); // Utiliser le premier type par défaut

            // Migration des données : mapper les anciens enum vers les nouveaux IDs
            // 0 = FicheTechnique -> ID 1, 1 = Nuancier -> ID 2, etc.
            migrationBuilder.Sql(@"
                UPDATE ImportsPDF 
                SET TypeDocumentImportId = 
                    CASE TypeDocument 
                        WHEN 0 THEN 1  -- FicheTechnique
                        WHEN 1 THEN 2  -- Nuancier
                        WHEN 2 THEN 3  -- Brochure
                        WHEN 3 THEN 4  -- ClassFeu
                        WHEN 4 THEN 5  -- ClassUPEC
                        ELSE 6         -- Autre
                    END
            ");

            // Supprimer l'ancienne colonne
            migrationBuilder.DropColumn(
                name: "TypeDocument",
                table: "ImportsPDF");

            migrationBuilder.CreateIndex(
                name: "IX_ImportsPDF_TypeDocumentImportId",
                table: "ImportsPDF",
                column: "TypeDocumentImportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportsPDF_TypesDocuments_TypeDocumentImportId",
                table: "ImportsPDF",
                column: "TypeDocumentImportId",
                principalTable: "TypesDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportsPDF_TypesDocuments_TypeDocumentImportId",
                table: "ImportsPDF");

            migrationBuilder.DropIndex(
                name: "IX_ImportsPDF_TypeDocumentImportId",
                table: "ImportsPDF");

            migrationBuilder.RenameColumn(
                name: "TypeDocumentImportId",
                table: "ImportsPDF",
                newName: "TypeDocument");

            migrationBuilder.CreateTable(
                name: "SectionLibreSectionConteneurs",
                columns: table => new
                {
                    SectionLibresId = table.Column<int>(type: "int", nullable: false),
                    SectionConteneursId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionLibreSectionConteneurs", x => new { x.SectionLibresId, x.SectionConteneursId });
                    table.ForeignKey(
                        name: "FK_SectionLibreSectionConteneurs_SectionsConteneurs_SectionConteneursId",
                        column: x => x.SectionConteneursId,
                        principalTable: "SectionsConteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionLibreSectionConteneurs_SectionsLibres_SectionLibresId",
                        column: x => x.SectionLibresId,
                        principalTable: "SectionsLibres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectionLibreSectionConteneurs_SectionConteneursId",
                table: "SectionLibreSectionConteneurs",
                column: "SectionConteneursId");
        }
    }
}
