using Artikelplanung.Web.Services.Import;

namespace Artikelplanung.Tests;

/// <summary>Prüft gegen eine echte Herstellerliste (SIC! Longfill, siehe TestData/), damit der
/// Import nicht nur an einer künstlichen Testdatei hängt.</summary>
public class SpreadsheetReaderTests
{
    private static string FixturePath => Path.Combine(AppContext.BaseDirectory, "TestData", "SIC-Longfill.xlsx");

    [Fact]
    public void Read_ErkenntKopfzeileUndAlleDatenzeilen()
    {
        using var stream = File.OpenRead(FixturePath);

        var table = SpreadsheetReader.Read(stream);

        Assert.Equal(
            ["Artikel-Nr.", "Bezeichnung", "Geschmack", "NettoEKFest", "BruttoVKFest", "Menge", "VPE", "EANNummer", "Aktion"],
            table.Headers);
        Assert.Equal(8, table.Rows.Count);
    }

    [Fact]
    public void Read_ListetSpaltenwerteInDerRichtigenSpaltenreihenfolge()
    {
        using var stream = File.OpenRead(FixturePath);

        var table = SpreadsheetReader.Read(stream);
        var ersteZeile = table.Rows[0];

        Assert.Equal("SIC301", ersteZeile[0]);
        Assert.Contains("Elderflower", ersteZeile[1]);
        Assert.Equal("5902811659987", ersteZeile[7]);
    }
}
