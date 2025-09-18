using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePageGardeTemplateIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageGardeTemplateId",
                table: "DocumentsGeneres",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentsGeneres_PageGardeTemplateId",
                table: "DocumentsGeneres",
                column: "PageGardeTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentsGeneres_PageGardeTemplates_PageGardeTemplateId",
                table: "DocumentsGeneres",
                column: "PageGardeTemplateId",
                principalTable: "PageGardeTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentsGeneres_PageGardeTemplates_PageGardeTemplateId",
                table: "DocumentsGeneres");

            migrationBuilder.DropIndex(
                name: "IX_DocumentsGeneres_PageGardeTemplateId",
                table: "DocumentsGeneres");

            migrationBuilder.DropColumn(
                name: "PageGardeTemplateId",
                table: "DocumentsGeneres");
        }
    }
}
