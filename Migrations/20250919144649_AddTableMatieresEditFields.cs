using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddTableMatieresEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EditTableMatieres",
                table: "DocumentsGeneres",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TableMatieresPersonnalisee",
                table: "DocumentsGeneres",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditTableMatieres",
                table: "DocumentsGeneres");

            migrationBuilder.DropColumn(
                name: "TableMatieresPersonnalisee",
                table: "DocumentsGeneres");
        }
    }
}
