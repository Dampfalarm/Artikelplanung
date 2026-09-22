using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Artikelplanung.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarkeUndArchiviert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Archiviert",
                table: "ArtikelEintraege",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Marke",
                table: "ArtikelEintraege",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Archiviert",
                table: "ArtikelEintraege");

            migrationBuilder.DropColumn(
                name: "Marke",
                table: "ArtikelEintraege");
        }
    }
}
