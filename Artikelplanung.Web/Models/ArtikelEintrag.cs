namespace Artikelplanung.Web.Models;

/// <summary>Ein Eintrag in der Artikelplanung – ein Artikel(-Vorhaben), keine Excel-Zeile. Ein
/// Import fügt diesem einen Eintrag eine ganze Tabelle als Referenz hinzu (siehe
/// <see cref="ImportTabelleJson"/>), er legt keine weiteren Einträge an: eine Herstellerliste mit
/// z. B. acht Geschmacksrichtungen gehört fachlich zu einem Anlage-Vorhaben, nicht zu acht.</summary>
public class ArtikelEintrag
{
    public int Id { get; set; }

    public required string Artikelname { get; set; }

    public string? Ean { get; set; }

    public Prioritaet Prioritaet { get; set; } = Prioritaet.Mittel;

    public ArtikelStatus Status { get; set; } = ArtikelStatus.Offen;

    public DateOnly? ReleaseDatum { get; set; }

    public required string EingetragenVon { get; set; }

    public string? Notiz { get; set; }

    /// <summary>Dateiname der angehängten Excel-Liste, falls vorhanden.</summary>
    public string? Quelle { get; set; }

    /// <summary>Komplette angehängte Excel-Tabelle (Kopfzeile + Zeilen) als JSON
    /// (Services/Import/SpreadsheetTable), roh und unverändert – nicht strukturiert, weil jede
    /// Herstellerliste andere Spalten mitbringt. Null, solange nichts angehängt ist.</summary>
    public string? ImportTabelleJson { get; set; }

    public DateTime ErstelltAm { get; set; }
}
