using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerateurDOE.Migrations
{
    /// <inheritdoc />
    public partial class AddPageGardeTemplatesOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nettoyage optionnel des colonnes obsolètes (ignoré si elles n'existent pas)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SectionsLibres_SectionsConteneurs_SectionConteneurId')
                    ALTER TABLE [SectionsLibres] DROP CONSTRAINT [FK_SectionsLibres_SectionsConteneurs_SectionConteneurId];

                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SectionsLibres_SectionsConteneurs_SectionConteneurId1')
                    ALTER TABLE [SectionsLibres] DROP CONSTRAINT [FK_SectionsLibres_SectionsConteneurs_SectionConteneurId1];

                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SectionsLibres_SectionConteneurId')
                    DROP INDEX [IX_SectionsLibres_SectionConteneurId] ON [SectionsLibres];

                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SectionsLibres_SectionConteneurId1')
                    DROP INDEX [IX_SectionsLibres_SectionConteneurId1] ON [SectionsLibres];

                IF COL_LENGTH('SectionsLibres', 'SectionConteneurId') IS NOT NULL
                    ALTER TABLE [SectionsLibres] DROP COLUMN [SectionConteneurId];

                IF COL_LENGTH('SectionsLibres', 'SectionConteneurId1') IS NOT NULL
                    ALTER TABLE [SectionsLibres] DROP COLUMN [SectionConteneurId1];
            ");

            migrationBuilder.CreateTable(
                name: "PageGardeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContenuHtml = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    ContenuJson = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: false),
                    EstParDefaut = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageGardeTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageGardeTemplates_Nom",
                table: "PageGardeTemplates",
                column: "Nom",
                unique: true);

            // Insertion du template par défaut
            migrationBuilder.InsertData(
                table: "PageGardeTemplates",
                columns: new[] { "Nom", "Description", "ContenuHtml", "ContenuJson", "EstParDefaut" },
                values: new object[] {
                    "Template Par Défaut",
                    "Template de page de garde par défaut avec design moderne",
                    @"<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 60px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            text-align: center;
            box-sizing: border-box;
        }
        .main-title {
            font-size: 3em;
            font-weight: 300;
            margin-bottom: 40px;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
            line-height: 1.2;
        }
        .project-info {
            background: rgba(255,255,255,0.1);
            padding: 40px;
            border-radius: 15px;
            margin: 40px 0;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255,255,255,0.2);
        }
        .info-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: 15px 0;
            font-size: 1.1em;
            flex-wrap: wrap;
        }
        .info-row .label {
            font-weight: 600;
            margin-right: 20px;
            min-width: 150px;
            text-align: left;
        }
        .info-row .value {
            flex: 1;
            text-align: right;
            word-break: break-word;
        }
        .company-info {
            margin-top: 60px;
            font-size: 1.3em;
            font-weight: 500;
            opacity: 0.9;
        }
        .date {
            position: absolute;
            bottom: 40px;
            right: 40px;
            font-size: 1em;
            opacity: 0.8;
            font-weight: 300;
        }
        @media print {
            body {
                height: 297mm;
                width: 210mm;
                padding: 20mm;
            }
            .date {
                bottom: 20mm;
                right: 20mm;
            }
        }
    </style>
</head>
<body>
    <h1 class='main-title'>{{document.type}}</h1>

    <div class='project-info'>
        <div class='info-row'>
            <span class='label'>Projet :</span>
            <span class='value'>{{chantier.nomProjet}}</span>
        </div>
        <div class='info-row'>
            <span class='label'>Maître d'œuvre :</span>
            <span class='value'>{{chantier.maitreOeuvre}}</span>
        </div>
        <div class='info-row'>
            <span class='label'>Maître d'ouvrage :</span>
            <span class='value'>{{chantier.maitreOuvrage}}</span>
        </div>
        <div class='info-row'>
            <span class='label'>Adresse :</span>
            <span class='value'>{{chantier.adresse}}</span>
        </div>
        <div class='info-row'>
            <span class='label'>Lot :</span>
            <span class='value'>{{document.numeroLot}} - {{document.intituleLot}}</span>
        </div>
    </div>

    <div class='company-info'>
        <strong>{{system.nomEntreprise}}</strong>
    </div>

    <div class='date'>
        {{system.date}}
    </div>
</body>
</html>",
                    @"{""backgroundColor"": ""linear-gradient(135deg, #667eea 0%, #764ba2 100%)"", ""fontFamily"": ""'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"", ""textColor"": ""white"", ""showLogo"": false, ""logoPath"": """", ""variables"": [""document.type"", ""document.numeroLot"", ""document.intituleLot"", ""chantier.nomProjet"", ""chantier.maitreOeuvre"", ""chantier.maitreOuvrage"", ""chantier.adresse"", ""system.nomEntreprise"", ""system.date""]}",
                    true
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageGardeTemplates");

            migrationBuilder.AddColumn<int>(
                name: "SectionConteneurId",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionConteneurId1",
                table: "SectionsLibres",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_SectionConteneurId",
                table: "SectionsLibres",
                column: "SectionConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionsLibres_SectionConteneurId1",
                table: "SectionsLibres",
                column: "SectionConteneurId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId",
                table: "SectionsLibres",
                column: "SectionConteneurId",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionsLibres_SectionsConteneurs_SectionConteneurId1",
                table: "SectionsLibres",
                column: "SectionConteneurId1",
                principalTable: "SectionsConteneurs",
                principalColumn: "Id");
        }
    }
}
