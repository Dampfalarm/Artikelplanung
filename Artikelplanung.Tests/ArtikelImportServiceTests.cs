using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Data;
using Artikelplanung.Web.Models;
using Artikelplanung.Web.Services.Import;

namespace Artikelplanung.Tests;

/// <summary>Ein Import hängt eine ganze Tabelle an genau einen Artikel an – er legt keine
/// weiteren Artikel an, auch wenn die Quelldatei mehrere Zeilen hat (siehe ArtikelEintrag.cs).</summary>
public class ArtikelImportServiceTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "TestData", "SIC-Longfill.xlsx");

    private static async Task<(DbContextOptions<ApplicationDbContext> Options, SqliteConnection Connection)> NeueTestDatenbankAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var setup = new ApplicationDbContext(options);
        await setup.Database.EnsureCreatedAsync();
        return (options, connection);
    }

    [Fact]
    public async Task AttachAsync_HaengtDieKompletteTabelleAnGenauEinenArtikelAn()
    {
        var (options, connection) = await NeueTestDatenbankAsync();
        await using var _ = connection;

        await using (var db = new ApplicationDbContext(options))
        {
            db.ArtikelEintraege.Add(new ArtikelEintrag { Artikelname = "SIC Longfill Serie", EingetragenVon = "Benny" });
            await db.SaveChangesAsync();
        }

        using var stream = File.OpenRead(FixturePath);
        var table = SpreadsheetReader.Read(stream);
        var service = new ArtikelImportService(new TestDbContextFactory(options));

        int artikelId;
        await using (var db = new ApplicationDbContext(options))
        {
            artikelId = (await db.ArtikelEintraege.SingleAsync()).Id;
        }

        await service.AttachAsync(artikelId, table, "SIC-Longfill.xlsx");

        await using var pruef = new ApplicationDbContext(options);
        var gespeichert = await pruef.ArtikelEintraege.ToListAsync();

        // Genau ein Artikel bleibt bestehen, auch wenn die Quelldatei 8 Zeilen hatte.
        Assert.Single(gespeichert);
        var eintrag = gespeichert[0];
        Assert.Equal("SIC-Longfill.xlsx", eintrag.Quelle);
        Assert.NotNull(eintrag.ImportTabelleJson);

        var gespeicherteTabelle = SpreadsheetTable.FromJson(eintrag.ImportTabelleJson!);
        Assert.Equal(8, gespeicherteTabelle.Rows.Count);
        Assert.Equal(table.Headers, gespeicherteTabelle.Headers);
    }

    [Fact]
    public async Task AttachAsync_ErsetztEineVorherAngehaengteTabelle()
    {
        var (options, connection) = await NeueTestDatenbankAsync();
        await using var _ = connection;

        int artikelId;
        await using (var db = new ApplicationDbContext(options))
        {
            var eintrag = new ArtikelEintrag { Artikelname = "Testartikel", EingetragenVon = "Benny" };
            db.ArtikelEintraege.Add(eintrag);
            await db.SaveChangesAsync();
            artikelId = eintrag.Id;
        }

        var service = new ArtikelImportService(new TestDbContextFactory(options));
        var ersteTabelle = new SpreadsheetTable { Headers = ["A"], Rows = [["1"]] };
        var zweiteTabelle = new SpreadsheetTable { Headers = ["B", "C"], Rows = [["x", "y"], ["z", "w"]] };

        await service.AttachAsync(artikelId, ersteTabelle, "erste.xlsx");
        await service.AttachAsync(artikelId, zweiteTabelle, "zweite.xlsx");

        await using var pruef = new ApplicationDbContext(options);
        var eintragNachher = await pruef.ArtikelEintraege.SingleAsync();
        Assert.Equal("zweite.xlsx", eintragNachher.Quelle);
        Assert.Equal(zweiteTabelle.Headers, SpreadsheetTable.FromJson(eintragNachher.ImportTabelleJson!).Headers);
    }

    [Fact]
    public async Task AttachAsync_WirftBeiUnbekannterArtikelId()
    {
        var (options, connection) = await NeueTestDatenbankAsync();
        await using var _ = connection;

        var service = new ArtikelImportService(new TestDbContextFactory(options));
        var table = new SpreadsheetTable { Headers = ["A"], Rows = [["1"]] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AttachAsync(999, table, "datei.xlsx"));
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
