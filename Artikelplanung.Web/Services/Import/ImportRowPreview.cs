namespace Artikelplanung.Web.Services.Import;

/// <summary>Eine Zeile aus einer eingelesenen Tabelle, bereits nach Spaltenzuordnung aufgeteilt:
/// Artikelname/EAN für die Tabellen-Hauptspalten, der Rest fürs Ausklapp-Feld.</summary>
public sealed record ImportRowPreview(
    string Artikelname,
    string? Ean,
    IReadOnlyList<(string Spaltenname, string? Wert)> UebrigeSpalten);
