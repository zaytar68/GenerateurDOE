using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSectionLibreToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneursId",
                table: "SectionsLibres");

            migrationBuilder.DropIndex(
                name: "IX_SectionsLibres_SectionConteneursId",
                table: "SectionsLibres");

            migrationBuilder.DropColumn(
                name: "SectionConteneursId",
                table: "SectionsLibres");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectionLibreSectionConteneurs");

            migrationBuilder.AddColumn<int>(
                name: "SectionConteneursId",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_SectionConteneursId",
                table: "SectionsLibres",
                column: "SectionConteneursId");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneursId",
                table: "SectionsLibres",
                column: "SectionConteneursId",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
