using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Data;
using Artikelplanung.Web.Models;

namespace Artikelplanung.Web.Services.Import;

public class ArtikelImportService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public static SpreadsheetTable ReadXlsx(Stream stream) => SpreadsheetReader.Read(stream);

    /// <summary>Teilt jede Zeile in Artikelname/EAN (feste Spalten) und den Rest (fürs
    /// Ausklapp-Feld) auf, ohne etwas zu speichern – für die Vorschau vor dem Import.</summary>
    public static List<ImportRowPreview> BuildPreview(
        SpreadsheetTable table, string artikelnameSpalte, string? eanSpalte)
    {
        var artikelnameIndex = table.Headers.ToList().IndexOf(artikelnameSpalte);
        if (artikelnameIndex < 0)
        {
            throw new ArgumentException($"Spalte \"{artikelnameSpalte}\" nicht gefunden.", nameof(artikelnameSpalte));
        }
        var eanIndex = eanSpalte is null ? -1 : table.Headers.ToList().IndexOf(eanSpalte);

        var previews = new List<ImportRowPreview>();
        foreach (var row in table.Rows)
        {
            var artikelname = artikelnameIndex < row.Count ? row[artikelnameIndex] : null;
            if (string.IsNullOrWhiteSpace(artikelname)) continue;

            var ean = eanIndex >= 0 && eanIndex < row.Count ? row[eanIndex] : null;

            var uebrige = new List<(string, string?)>();
            for (var i = 0; i < table.Headers.Count; i++)
            {
                if (i == artikelnameIndex || i == eanIndex) continue;
                var wert = i < row.Count ? row[i] : null;
                uebrige.Add((table.Headers[i], wert));
            }

            previews.Add(new ImportRowPreview(artikelname.Trim(), ean?.Trim(), uebrige));
        }

        return previews;
    }

    public async Task<int> ImportAsync(
        List<ImportRowPreview> rows,
        string dateiname,
        string eingetragenVon,
        Prioritaet prioritaet,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var jetzt = DateTime.Now;

        foreach (var row in rows)
        {
            var eintrag = new ArtikelEintrag
            {
                Artikelname = row.Artikelname,
                Ean = string.IsNullOrWhiteSpace(row.Ean) ? null : row.Ean,
                Prioritaet = prioritaet,
                Status = ArtikelStatus.Offen,
                EingetragenVon = eingetragenVon,
                Quelle = dateiname,
                ErstelltAm = jetzt,
            };

            var reihenfolge = 0;
            foreach (var (spaltenname, wert) in row.UebrigeSpalten)
            {
                eintrag.ImportierteSpalten.Add(new ImportierteSpalte
                {
                    Spaltenname = spaltenname,
                    Wert = wert,
                    Reihenfolge = reihenfolge++,
                });
            }

            db.ArtikelEintraege.Add(eintrag);
        }

        // Bewusst rows.Count statt des SaveChangesAsync-Rückgabewerts: der zählt jede betroffene
        // Zeile inklusive der ImportierteSpalten mit, nicht nur die neu angelegten Artikel.
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }
}
