namespace Artikelplanung.Web.Services;

/// <summary>Das Datenverzeichnis der Anwendung: SQLite-Datenbank. Lokal &lt;Projekt&gt;/Data, auf
/// einem Server über "DataDirectory" (appsettings.Production.json) ein fester Pfad außerhalb des
/// Programmordners, damit Updates die Daten nicht anfassen (siehe Rechnungsablage docs/deployment.md
/// für das gleiche Muster). Bewusst nie relativ zum Arbeitsverzeichnis: als Windows-Dienst wäre
/// das C:\Windows\System32.</summary>
public sealed class DataPaths(string directory)
{
    public string Directory { get; } = directory;

    public string Database => Path.Combine(Directory, "app.db");

    public static DataPaths Resolve(IConfiguration configuration, string contentRootPath)
    {
        var configured = configuration["DataDirectory"];
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(contentRootPath, "Data")
            : Path.GetFullPath(configured, contentRootPath);
        return new DataPaths(directory);
    }
}
