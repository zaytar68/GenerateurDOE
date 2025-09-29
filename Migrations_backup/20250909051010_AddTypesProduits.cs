using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddTypesProduits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TypeProduitId",
                table: "FichesTechniques",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TypesProduits",
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
                    table.PrimaryKey("PK_TypesProduits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichesTechniques_TypeProduitId",
                table: "FichesTechniques",
                column: "TypeProduitId");

            migrationBuilder.AddForeignKey(
                name: "FK_FichesTechniques_TypesProduits_TypeProduitId",
                table: "FichesTechniques",
                column: "TypeProduitId",
                principalTable: "TypesProduits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichesTechniques_TypesProduits_TypeProduitId",
                table: "FichesTechniques");

            migrationBuilder.DropTable(
                name: "TypesProduits");

            migrationBuilder.DropIndex(
                name: "IX_FichesTechniques_TypeProduitId",
                table: "FichesTechniques");

            migrationBuilder.DropColumn(
                name: "TypeProduitId",
                table: "FichesTechniques");
        }
    }
}
