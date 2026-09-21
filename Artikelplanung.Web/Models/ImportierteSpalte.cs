namespace Artikelplanung.Web.Models;

/// <summary>Eine Roh-Spalte aus einer importierten Excel-Zeile, die keiner festen Spalte
/// zugeordnet wurde (z. B. Preis, Geschmack, Aktion). Reihenfolge hält die Spaltenreihenfolge
/// der Quelldatei fürs Ausklapp-Feld in der Liste.</summary>
public class ImportierteSpalte
{
    public int Id { get; set; }

    public int ArtikelEintragId { get; set; }

    public required string Spaltenname { get; set; }

    public string? Wert { get; set; }

    public int Reihenfolge { get; set; }
}
