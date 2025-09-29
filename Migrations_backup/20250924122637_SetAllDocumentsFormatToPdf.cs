using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class SetAllDocumentsFormatToPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mettre à jour tous les documents existants pour utiliser le format PDF (valeur enum = 0)
            migrationBuilder.Sql(
                "UPDATE DocumentsGeneres SET FormatExport = 0 WHERE FormatExport != 0",
                suppressTransaction: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
