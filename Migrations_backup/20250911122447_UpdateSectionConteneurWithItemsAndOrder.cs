using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSectionConteneurWithItemsAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionConteneurId",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionConteneurId1",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SectionConteneurItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    SectionConteneursId = table.Column<int>(type: "int", nullable: false),
                    SectionLibreId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_SectionConteneurId",
                table: "SectionsLibres",
                column: "SectionConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_SectionConteneurId1",
                table: "SectionsLibres",
                column: "SectionConteneurId1");

            migrationBuilder.CreateIndex(
                name: "IX_SectionConteneurItems_SectionConteneursId_SectionLibreId",
                table: "SectionConteneurItems",
                columns: new[] { "SectionConteneursId", "SectionLibreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionConteneurItems_SectionLibreId",
                table: "SectionConteneurItems",
                column: "SectionLibreId");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId",
                table: "SectionsLibres",
                column: "SectionConteneurId",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId1",
                table: "SectionsLibres",
                column: "SectionConteneurId1",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId",
                table: "SectionsLibres");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId1",
                table: "SectionsLibres");

            migrationBuilder.DropTable(
                name: "SectionConteneurItems");

            migrationBuilder.DropIndex(
                name: "IX_SectionsLibres_SectionConteneurId",
                table: "SectionsLibres");

            migrationBuilder.DropIndex(
                name: "IX_SectionsLibres_SectionConteneurId1",
                table: "SectionsLibres");

            migrationBuilder.DropColumn(
                name: "SectionConteneurId",
                table: "SectionsLibres");

            migrationBuilder.DropColumn(
                name: "SectionConteneurId1",
                table: "SectionsLibres");
        }
    }
}
