# Artikelplanung

## Status

🛠️ **v1 gebaut, lokal getestet.** Liste mit manueller Anlage und Excel-Import stehen.
Rollout-Anleitung steht ([docs/deployment.md](docs/deployment.md) als Referenz, kopierfertige Befehle im
Artefakt [„Artikelplanung Server-Rollout"](https://claude.ai/artifact/R6v2VSurXsxVXWxHu3nBkg)), Ausrollen
auf MSDC02 steht noch aus.

## Grundidee

Aktuell kursieren mehrere Excel-Listen für neue Artikel im Team-Chat (v. a. Dampfalarm) –
unübersichtlich, keine gemeinsame Priorisierung, kein Blick auf anstehende Release-Termine.
Dieses Tool ist der eine Ort dafür: Artikel eintragen oder aus einer Herstellerliste
importieren, Priorität setzen, optional ein Release-Datum, Status verfolgen.

## Entschiedene Punkte (21.09.2026)

- **Kein Login.** Nur im Firmennetz erreichbar, das reicht als Zugriffsschutz. Stattdessen
  wählt man beim ersten Öffnen aus einer festen Namensliste (Florian, Benny, Michael, Simon,
  Heiko, Eren, Dominik), wer gerade am PC sitzt; ein Browser-Cookie merkt sich das (1 Jahr,
  `PersonenAuswahl.razor` + `wwwroot/js/person-cookie.js`), "Eingetragen von" füllt sich danach
  automatisch, mit sichtbarem "wechseln"-Link oben rechts. Keine Zugriffssteuerung, nur Zuordnung.
- **Priorität**: Hoch / Mittel / Niedrig, keine feinere Stufung.
- **Ein Import ist ein Anhang an einen Artikel, keine Artikel-Fabrik.** Eine Herstellerliste mit
  z. B. acht Geschmacksrichtungen legt nicht acht Zeilen in der Planung an, sondern hängt ihre
  komplette Tabelle unverändert an den einen Artikel(-Vorhaben), zu dem sie gehört – anhängbar
  beim Neuanlegen oder später bei jedem bestehenden Artikel. Im Ausklapp-Feld erscheint sie dann
  als vollständige Tabelle (siehe Testfixture `Artikelplanung.Tests/TestData/SIC-Longfill.xlsx`).
  Bewusst kein festes Spaltenschema dafür, weil jede Herstellerliste andere Spalten mitbringt.
  Deshalb auch kein eigenes EAN-Feld am Artikel mehr: Ein Planungs-Eintrag kann mehrere EANs
  über den Import mitbringen (z. B. eine je Geschmacksrichtung), die stehen dann in der
  angehängten Tabelle.
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

`ArtikelEintrag`: Artikelname, Priorität, Status, Release-Datum (optional), eingetragen von,
Notiz, Import-Quelle (Dateiname) und `ImportTabelleJson` – die komplette angehängte
Excel-Tabelle (Kopfzeile + Zeilen) als JSON, höchstens eine pro Artikel, ein erneuter Import
ersetzt die vorherige. Kein eigenes EAN-Feld (siehe oben).

## Starten

```
cd Artikelplanung.Web
dotnet run
```

SQLite-Datenbank liegt dann unter `Artikelplanung.Web/Data/app.db` (nicht versioniert).
