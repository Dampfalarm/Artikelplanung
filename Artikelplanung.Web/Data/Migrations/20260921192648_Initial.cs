using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Artikelplanung.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtikelEintraege",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Artikelname = table.Column<string>(type: "TEXT", nullable: false),
                    Ean = table.Column<string>(type: "TEXT", nullable: true),
                    Prioritaet = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleaseDatum = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EingetragenVon = table.Column<string>(type: "TEXT", nullable: false),
                    Notiz = table.Column<string>(type: "TEXT", nullable: true),
                    Quelle = table.Column<string>(type: "TEXT", nullable: true),
                    ImportTabelleJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErstelltAm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtikelEintraege", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtikelEintraege_Ean",
                table: "ArtikelEintraege",
                column: "Ean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtikelEintraege");
        }
    }
}
