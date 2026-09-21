using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Data;
using Artikelplanung.Web.Models;
using Artikelplanung.Web.Services.Import;

namespace Artikelplanung.Tests;

public class ArtikelImportServiceTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "TestData", "SIC-Longfill.xlsx");

    [Fact]
    public void BuildPreview_TeiltArtikelnameUndEanAbUndBehaeltDenRestAlsUebrigeSpalten()
    {
        using var stream = File.OpenRead(FixturePath);
        var table = SpreadsheetReader.Read(stream);

        var preview = ArtikelImportService.BuildPreview(table, "Bezeichnung", "EANNummer");

        Assert.Equal(8, preview.Count);
        Assert.Contains(preview, p => p.Artikelname.Contains("Elderflower") && p.Ean == "5902811659987");
        // 9 Spalten insgesamt, 2 davon (Bezeichnung, EANNummer) sind feste Spalten -> 7 bleiben übrig.
        Assert.All(preview, p => Assert.Equal(7, p.UebrigeSpalten.Count));
        Assert.All(preview, p => Assert.DoesNotContain(p.UebrigeSpalten, s => s.Spaltenname is "Bezeichnung" or "EANNummer"));
    }

    [Fact]
    public void BuildPreview_OhneEanSpalteLaesstEanLeer()
    {
        using var stream = File.OpenRead(FixturePath);
        var table = SpreadsheetReader.Read(stream);

        var preview = ArtikelImportService.BuildPreview(table, "Bezeichnung", eanSpalte: null);

        Assert.All(preview, p => Assert.Null(p.Ean));
        Assert.All(preview, p => Assert.Equal(8, p.UebrigeSpalten.Count));
    }

    [Fact]
    public void BuildPreview_UeberspringtZeilenOhneArtikelname()
    {
        var table = new SpreadsheetTable
        {
            Headers = ["Name", "EAN"],
            Rows = [["Artikel A", "111"], [null, "222"], ["  ", "333"], ["Artikel B", "444"]],
        };

        var preview = ArtikelImportService.BuildPreview(table, "Name", "EAN");

        Assert.Equal(["Artikel A", "Artikel B"], preview.Select(p => p.Artikelname));
    }

    [Fact]
    public async Task ImportAsync_LegtProZeileEinenArtikelMitImportiertenSpaltenAn()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        using var stream = File.OpenRead(FixturePath);
        var table = SpreadsheetReader.Read(stream);
        var preview = ArtikelImportService.BuildPreview(table, "Bezeichnung", "EANNummer");

        var factory = new TestDbContextFactory(options);
        var service = new ArtikelImportService(factory);

        var anzahl = await service.ImportAsync(preview, "SIC-Longfill.xlsx", "Benny", Prioritaet.Hoch);

        Assert.Equal(8, anzahl);

        await using var db = new ApplicationDbContext(options);
        var gespeichert = await db.ArtikelEintraege.Include(a => a.ImportierteSpalten).ToListAsync();
        Assert.Equal(8, gespeichert.Count);
        Assert.All(gespeichert, a =>
        {
            Assert.Equal(Prioritaet.Hoch, a.Prioritaet);
            Assert.Equal(ArtikelStatus.Offen, a.Status);
            Assert.Equal("Benny", a.EingetragenVon);
            Assert.Equal("SIC-Longfill.xlsx", a.Quelle);
            Assert.Equal(7, a.ImportierteSpalten.Count);
        });
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
