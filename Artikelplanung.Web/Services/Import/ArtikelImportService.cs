using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Data;

namespace Artikelplanung.Web.Services.Import;

/// <summary>Hängt eine eingelesene Excel-Tabelle an genau einen ArtikelEintrag – ein Import legt
/// keine weiteren Artikel an, er ergänzt einen bestehenden oder gerade neu angelegten um seine
/// Rohdaten fürs Ausklapp-Feld.</summary>
public class ArtikelImportService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public static SpreadsheetTable ReadXlsx(Stream stream) => SpreadsheetReader.Read(stream);

    public async Task AttachAsync(int artikelId, SpreadsheetTable table, string dateiname, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var eintrag = await db.ArtikelEintraege.FindAsync([artikelId], ct)
            ?? throw new InvalidOperationException($"Artikel {artikelId} wurde nicht gefunden.");

        eintrag.ImportTabelleJson = table.ToJson();
        eintrag.Quelle = dateiname;
        await db.SaveChangesAsync(ct);
    }
}
