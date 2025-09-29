using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrationPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ContenuJson",
                table: "SectionsLibres",
                type: "text",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuHtml",
                table: "SectionsLibres",
                type: "text",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuJson",
                table: "PageGardeTemplates",
                type: "text",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuHtml",
                table: "PageGardeTemplates",
                type: "text",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 2147483647);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ContenuJson",
                table: "SectionsLibres",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuHtml",
                table: "SectionsLibres",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuJson",
                table: "PageGardeTemplates",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 2147483647);

            migrationBuilder.AlterColumn<string>(
                name: "ContenuHtml",
                table: "PageGardeTemplates",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 2147483647);
        }
    }
}
