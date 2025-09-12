using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelAfterLotMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntituleLot",
                table: "Chantiers");

            migrationBuilder.DropColumn(
                name: "NumeroLot",
                table: "Chantiers");

            migrationBuilder.AddColumn<string>(
                name: "IntituleLot",
                table: "DocumentsGeneres",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroLot",
                table: "DocumentsGeneres",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntituleLot",
                table: "DocumentsGeneres");

            migrationBuilder.DropColumn(
                name: "NumeroLot",
                table: "DocumentsGeneres");

            migrationBuilder.AddColumn<string>(
                name: "IntituleLot",
                table: "Chantiers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroLot",
                table: "Chantiers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
