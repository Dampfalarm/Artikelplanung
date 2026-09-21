namespace Artikelplanung.Web.Models;

/// <summary>Ein Eintrag in der Artikelplanung – manuell angelegt oder aus einer Herstellerliste
/// importiert. EAN und Artikelname sind eigene Spalten, weil danach gesucht/abgeglichen wird;
/// alles andere aus einem Import (Preise, Geschmack, Aktionen, ...) landet unstrukturiert in
/// <see cref="ImportierteSpalten"/>, weil jede Herstellerliste andere Spalten mitbringt.</summary>
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

    /// <summary>Dateiname der Quelle, falls per Import angelegt; null bei manueller Anlage.</summary>
    public string? Quelle { get; set; }

    public DateTime ErstelltAm { get; set; }

    public List<ImportierteSpalte> ImportierteSpalten { get; set; } = [];
}
