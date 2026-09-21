# Artikelplanung

## Status

🛠️ **v1 gebaut, lokal getestet.** Liste mit manueller Anlage und Excel-Import stehen.
Rollout auf einen Server steht noch aus.

## Grundidee

Aktuell kursieren mehrere Excel-Listen für neue Artikel im Team-Chat (v. a. Dampfalarm) –
unübersichtlich, keine gemeinsame Priorisierung, kein Blick auf anstehende Release-Termine.
Dieses Tool ist der eine Ort dafür: Artikel eintragen oder aus einer Herstellerliste
importieren, Priorität setzen, optional ein Release-Datum, Status verfolgen.

## Entschiedene Punkte (21.09.2026)

- **Kein Login.** Nur im Firmennetz erreichbar, das reicht als Zugriffsschutz. Stattdessen
  ein freies "Eingetragen von"-Feld (mit Autovervollständigung aus bisherigen Werten) für
  Zuordnung, keine Zugriffssteuerung.
- **Priorität**: Hoch / Mittel / Niedrig, keine feinere Stufung.
- **Excel-Import ist spaltenflexibel.** Jede Herstellerliste bringt andere Spalten mit (siehe
  Testfixture `Artikelplanung.Tests/TestData/SIC-Longfill.xlsx`). Nur Artikelname und EAN sind
  feste Spalten in der Übersicht; alles andere aus einer importierten Zeile landet unverändert
  im Ausklapp-Bereich des jeweiligen Artikels, nicht in festen Datenbankspalten.
- **Gleicher Stack wie die Rechnungsablage** (Blazor Server + EF Core + SQLite, WAL-Modus,
  lauffähig als Windows-Dienst) – bewusst, weil das Muster sich dort schon bewährt hat und
  keine Cloud-Infra/-Kosten braucht.
- **Zurückgestellt, noch nicht gebaut:** Windows-Push-Benachrichtigungen (Alternative:
  Browser-Push, weniger Wartungsaufwand als eine verteilte Exe) und eine Kalenderansicht für
  Release-Termine (Datenfeld ist da, die View ist reiner Aufsatz für später). Ein
  projektübergreifendes MCP-Setup (Ecomhub, Rechnungsablage, Artikelplanung) für ein
  automatisiertes Wochen-Artefakt ist ebenfalls zurückgestellt, bis dieses Tool echte Daten hat.

## Struktur

```
Artikelplanung/
├── Artikelplanung.Web/          – Blazor-Server-Projekt (Models/, Data/, Services/, Components/)
├── Artikelplanung.Tests/        – xunit-Tests (Excel-Parsing, Import gegen echte Herstellerliste)
└── Artikelplanung.sln
```

## Datenmodell (Kurzfassung)

`ArtikelEintrag`: Artikelname, EAN, Priorität, Status, Release-Datum (optional), eingetragen
von, Notiz, Import-Quelle. Dazu `ImportierteSpalte` (Spaltenname/Wert/Reihenfolge) – beliebig
viele pro Artikel, für die Rohdaten aus einem Excel-Import.

## Starten

```
cd Artikelplanung.Web
dotnet run
```

SQLite-Datenbank liegt dann unter `Artikelplanung.Web/Data/app.db` (nicht versioniert).
