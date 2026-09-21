using Artikelplanung.Web.Services.Import;

namespace Artikelplanung.Tests;

public class SpreadsheetTableTests
{
    [Fact]
    public void ToJson_FromJson_RoundtripErhaeltHeaderZeilenUndLeereZellen()
    {
        var table = new SpreadsheetTable
        {
            Headers = ["Name", "EAN", "Notiz"],
            Rows = [["Artikel A", "111", null], ["Artikel B", null, "Sonderfall"]],
        };

        var wiederhergestellt = SpreadsheetTable.FromJson(table.ToJson());

        Assert.Equal(table.Headers, wiederhergestellt.Headers);
        Assert.Equal(table.Rows, wiederhergestellt.Rows);
    }
}
