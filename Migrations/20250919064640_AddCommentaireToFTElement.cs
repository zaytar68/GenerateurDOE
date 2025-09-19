using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentaireToFTElement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Commentaire",
                table: "FTElements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Commentaire",
                table: "FTElements");
        }
    }
}
