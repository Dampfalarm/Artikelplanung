namespace Artikelplanung.Web.Models;

/// <summary>Feste Liste der Leute, die Artikel eintragen – kein freies Textfeld mehr, damit über
/// die Browser-Cookie-Auswahl (Components/Shared/PersonenAuswahl.razor) immer ein sauberer,
/// bekannter Name landet.</summary>
public static class Personen
{
    public static readonly IReadOnlyList<string> Alle =
        ["Florian", "Benny", "Michael", "Simon", "Heiko", "Eren", "Dominik"];
}
