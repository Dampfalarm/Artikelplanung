namespace Artikelplanung.Web.Services;

/// <summary>Wer gerade an diesem Browser/Kreislauf angemeldet ist (PersonenAuswahl.razor liest das
/// beim ersten Rendern aus dem Cookie). Scoped, damit jede Blazor-Server-Sitzung ihren eigenen
/// Stand hat; andere Komponenten (z. B. das Anlage-Formular) hören auf <see cref="OnChange"/>, um
/// nach einem Personenwechsel sofort den richtigen Namen vorzuschlagen.</summary>
public class AktivePersonState
{
    public string? Name { get; private set; }

    public event Action? OnChange;

    public void Setzen(string name)
    {
        Name = name;
        OnChange?.Invoke();
    }

    /// <summary>"Person wechseln" in der Sidebar: setzt die Auswahl zurück, die Login-Karte
    /// erscheint erneut. Der Cookie bleibt unangetastet und wird bei der nächsten Auswahl
    /// überschrieben.</summary>
    public void Zuruecksetzen()
    {
        Name = null;
        OnChange?.Invoke();
    }
}
