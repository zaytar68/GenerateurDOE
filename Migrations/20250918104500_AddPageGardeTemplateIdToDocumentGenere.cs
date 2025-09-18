using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddPageGardeTemplateIdToDocumentGenere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageGardeTemplateId",
                table: "DocumentGeneres",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentGeneres_PageGardeTemplateId",
                table: "DocumentGeneres",
                column: "PageGardeTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentGeneres_PageGardeTemplates_PageGardeTemplateId",
                table: "DocumentGeneres",
                column: "PageGardeTemplateId",
                principalTable: "PageGardeTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentGeneres_PageGardeTemplates_PageGardeTemplateId",
                table: "DocumentGeneres");

            migrationBuilder.DropIndex(
                name: "IX_DocumentGeneres_PageGardeTemplateId",
                table: "DocumentGeneres");

            migrationBuilder.DropColumn(
                name: "PageGardeTemplateId",
                table: "DocumentGeneres");
        }
    }
}