using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionsLibres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichesTechniques_Chantiers_ChantierId",
                table: "FichesTechniques");

            migrationBuilder.CreateTable(
                name: "TypesSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionsLibres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    ContenuHtml = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    ContenuJson = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    TypeSectionId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_TypeSectionId",
                table: "SectionsLibres",
                column: "TypeSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_FichesTechniques_Chantiers_ChantierId",
                table: "FichesTechniques",
                column: "ChantierId",
                principalTable: "Chantiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichesTechniques_Chantiers_ChantierId",
                table: "FichesTechniques");

            migrationBuilder.DropTable(
                name: "SectionsLibres");

            migrationBuilder.DropTable(
                name: "TypesSections");

            migrationBuilder.AddForeignKey(
                name: "FK_FichesTechniques_Chantiers_ChantierId",
                table: "FichesTechniques",
                column: "ChantierId",
                principalTable: "Chantiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
