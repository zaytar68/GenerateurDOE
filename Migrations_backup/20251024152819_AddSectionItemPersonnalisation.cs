using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionItemPersonnalisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContenuHtmlPersonnalise",
                table: "SectionConteneurItems",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModificationPersonnalisation",
                table: "SectionConteneurItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitrePersonnalise",
                table: "SectionConteneurItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContenuHtmlPersonnalise",
                table: "SectionConteneurItems");

            migrationBuilder.DropColumn(
                name: "DateModificationPersonnalisation",
                table: "SectionConteneurItems");

            migrationBuilder.DropColumn(
                name: "TitrePersonnalise",
                table: "SectionConteneurItems");
        }
    }
}
