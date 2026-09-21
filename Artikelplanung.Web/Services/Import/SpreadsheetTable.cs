using System.Text.Json;

namespace Artikelplanung.Web.Services.Import;

/// <summary>Rohdaten einer eingelesenen Excel-Tabelle: erste nicht-leere Zeile als Kopfzeile,
/// jede folgende Zeile als Liste von Zellwerten in Spaltenreihenfolge (leere Zellen als null).
/// Bewusst ohne festes Schema, weil jede Herstellerliste andere Spalten mitbringt. Wird
/// unverändert als JSON an einem einzelnen ArtikelEintrag gespeichert (ArtikelEintrag.ImportTabelleJson).</summary>
public sealed class SpreadsheetTable
{
    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static SpreadsheetTable FromJson(string json) =>
        JsonSerializer.Deserialize<SpreadsheetTable>(json)
            ?? throw new InvalidDataException("Gespeicherte Import-Tabelle konnte nicht gelesen werden.");
}
