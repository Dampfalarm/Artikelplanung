# Deployment auf dem Firmenserver

Wie Artikelplanung auf dem Firmenserver läuft: rein intern, kein IIS, kein Zugriff von außen. Die App
läuft als **Windows-Dienst** über Kestrel, selbst-enthaltend veröffentlicht, auf dem Server ist also
nichts vorzuinstallieren – gleiches Muster wie die Rechnungsablage (siehe dort `docs/deployment.md`),
hier aber deutlich schlanker: kein Login, keine Netzlaufwerke, kein Mail-Import.

Die kopierfertigen Befehle mit ausgefüllten Platzhaltern stehen im Artefakt
**„Artikelplanung Server-Rollout"** (Link in der README). Dieses Dokument ist die Referenz dazu: was
passiert, warum, und was zu entscheiden ist.

Stand: 2026-09-22. Stellen mit `<…>` sind Platzhalter.

---

## 0. Entschieden

| Entscheidung    | Wert                                                                 | Warum                                                                                                                                                                                                    |
| --------------- | --------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Server**      | MSDC02 – gleicher Server wie die Rechnungsablage                     | Kein zweiter Server nötig, beide Apps laufen unabhängig als eigene Windows-Dienste nebeneinander.                                                                                                        |
| **Protokoll**   | HTTPS mit selbstsigniertem Zertifikat                                | Konsistent mit der Rechnungsablage; Aufwand (Zertifikat, Client-Vertrauen) ist überschaubar und einmalig.                                                                                                |
| **Ports**       | 8443 (HTTPS), 8080 (leitet auf HTTPS um)                              | Port 80/443 sind auf MSDC02 schon durch die Rechnungsablage belegt. Aufruf also mit Portangabe: `https://<HOST>:8443`.                                                                                  |
| **Dienstkonto** | `LocalSystem`                                                        | Die App schreibt nur lokal in ihr eigenes Datenverzeichnis (SQLite), keine Netzlaufwerke/Freigaben. Damit entfällt das AD-Konto samt „Anmelden als Dienst"-Recht komplett (das ist bei der Rechnungsablage der aufwändigste Teil). Der Starttyp `Automatic` sorgt unabhängig vom Konto dafür, dass der Dienst nach jedem Serverneustart von selbst wieder hochkommt. |
| **Pfade**       | alles unter `D:\Artikelplanung\`                                     | Programm, Daten, Zertifikat und Sicherungen getrennt, Updates fassen nur `app\` an. Laufwerk D:, weil C: auf MSDC02 knapp ist (gleicher Grund wie bei der Rechnungsablage).                              |
| **Zugriffsschutz** | keiner (kein Login) | Bewusste Entscheidung (siehe README) – Zugriffsschutz ist rein die Erreichbarkeit nur im Firmennetz. |

Ordnerstruktur auf dem Server:

```
D:\Artikelplanung\
  app\        Programm (wird bei Updates komplett ersetzt)
  data\       app.db – bleibt bei Updates unangetastet
  cert\       artikelplanung.pfx (+ .cer zum Verteilen)
  staging\    neues ZIP vor dem Update; "vorher\" = letzte Version als Rückweg
  backup\     nächtliche Kopien von data\
  deploy\     Update-Server.ps1, Backup-Data.ps1
```

Woher die App ihre Pfade kennt: `appsettings.Production.json` setzt `DataDirectory` auf
`D:/Artikelplanung/data` und den Zertifikatspfad auf `D:/Artikelplanung/cert/artikelplanung.pfx`. Die
Datenbank liegt dann in `data\app.db`. Wer andere Pfade will, ändert diese Datei **vor** dem
Veröffentlichen.

---

## 1. Server vorbereiten

Auf dem Server, PowerShell **als Administrator**:

```powershell
New-Item -ItemType Directory -Force -Path `
  D:\Artikelplanung\app, D:\Artikelplanung\data, D:\Artikelplanung\cert, `
  D:\Artikelplanung\staging, D:\Artikelplanung\backup, D:\Artikelplanung\deploy | Out-Null
```

Kein Dienstkonto anzulegen (siehe Abschnitt 0), also entfällt hier der komplette AD-Abschnitt der
Rechnungsablage-Anleitung.

**Ereignisquelle anlegen**, damit die Startmeldungen des Dienstes im Ereignisprotokoll landen:

```powershell
New-EventLog -LogName Application -Source "Artikelplanung.Web"
```

---

## 2. Zertifikat und DNS

**Selbstsigniert** (auf dem Server, als Administrator; `<HOST>` ist z. B. `artikelplanung.<domäne>.local`):

```powershell
$cert = New-SelfSignedCertificate -DnsName "<HOST>" -FriendlyName "Artikelplanung" `
  -CertStoreLocation "Cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(5)

$pfxPassword = Read-Host -AsSecureString "Export-Passwort für die PFX-Datei"
Export-PfxCertificate -Cert $cert -FilePath "D:\Artikelplanung\cert\artikelplanung.pfx" -Password $pfxPassword | Out-Null
Export-Certificate    -Cert $cert -FilePath "D:\Artikelplanung\cert\artikelplanung.cer" | Out-Null
```

Das Export-Passwort wird in Abschnitt 5 als Umgebungsvariable des Dienstes hinterlegt. **Nicht** in
`appsettings.json`, nicht ins Repo.

**Vertrauen auf den Clients**: die `.cer` in „Vertrauenswürdige Stammzertifizierungsstellen" der
Computer importieren, am einfachsten per Gruppenrichtlinie (wie bei der Rechnungsablage). Für einen
einzelnen Rechner:

```powershell
Import-Certificate -FilePath "\\<SERVER>\...\artikelplanung.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

**DNS**: CNAME `<HOST>` → Servername, auf dem DNS-Server:

```powershell
Add-DnsServerResourceRecordCName -ZoneName "<domäne>.local" -Name "artikelplanung" -HostNameAlias "<SERVER>.<domäne>.local"
```

Ablauf des Zertifikats: 5 Jahre. Datum notieren.

---

## 3. Veröffentlichen (auf dem Entwicklungsrechner)

Im Repo-Verzeichnis:

```powershell
dotnet publish Artikelplanung.Web -c Release -r win-x64 --self-contained true -o publish
Compress-Archive -Path publish\* -DestinationPath "Artikelplanung-$(Get-Date -Format yyyy-MM-dd).zip" -Force
```

Das ZIP (rund 100–150 MB, inklusive .NET-Laufzeit) enthält alles, was der Server braucht, auch den
Ordner `deploy\` mit den Skripten. `publish/` und die ZIPs sind in `.gitignore`.

Das ZIP nach `D:\Artikelplanung\staging\` auf den Server kopieren.

---

## 4. Erste Installation

Auf dem Server, als Administrator:

```powershell
Expand-Archive -Path (Get-ChildItem D:\Artikelplanung\staging\*.zip | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName `
  -DestinationPath D:\Artikelplanung\app -Force
Copy-Item D:\Artikelplanung\app\deploy\*.ps1 D:\Artikelplanung\deploy\ -Force
```

Für alle weiteren Versionen übernimmt das `Update-Server.ps1` (Abschnitt 8).

---

## 5. Dienst registrieren und Umgebungsvariable setzen

```powershell
New-Service -Name "Artikelplanung" -DisplayName "Artikelplanung" `
  -Description "Interne Artikelplanung (docs/deployment.md)" `
  -BinaryPathName "D:\Artikelplanung\app\Artikelplanung.Web.exe" `
  -StartupType Automatic

# Nach einem Absturz automatisch neu starten (nach 5 s, 30 s, 60 s; Zähler täglich zurück)
sc.exe failure Artikelplanung reset= 86400 actions= restart/5000/restart/30000/restart/60000
```

Ohne `-Credential` läuft der Dienst als `LocalSystem` (siehe Abschnitt 0) – kein weiteres Recht nötig.

**Zertifikatspasswort** als Umgebungsvariable nur für diesen Dienst (Registry, `Environment`,
mehrzeilige Zeichenfolge – damit sieht kein anderer Prozess auf dem Server das Passwort):

```powershell
$dienstUmgebung = @(
  "ASPNETCORE_ENVIRONMENT=Production",
  "Kestrel__Endpoints__Https__Certificate__Password=<PFX-PASSWORT>"
)
Set-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Services\Artikelplanung -Name Environment -Value $dienstUmgebung -Type MultiString
```

Weil die Befehle in der PowerShell-Historie landen, danach aufräumen:

```powershell
Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue; Clear-History
```

---

## 6. Firewall

Nur aus dem internen Netz, kein Port-Forwarding nach außen.

```powershell
New-NetFirewallRule -DisplayName "Artikelplanung HTTPS" -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow -Profile Domain,Private
New-NetFirewallRule -DisplayName "Artikelplanung HTTP (Umleitung)" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Domain,Private
```

---

## 7. Erster Start und Prüfung

```powershell
Start-Service Artikelplanung
Start-Sleep -Seconds 8
Get-Service Artikelplanung

# Startmeldungen: Migrationen
Get-WinEvent -LogName Application -MaxEvents 40 | Where-Object ProviderName -eq "Artikelplanung.Web" |
  Format-List TimeCreated, LevelDisplayName, Message

# Antwortet die App?
Invoke-WebRequest "https://<HOST>:8443" -UseBasicParsing | Select-Object StatusCode
```

Danach im Browser `https://<HOST>:8443` öffnen und die Personenauswahl treffen (siehe README, kein
Login).

---

## 8. Backup

`deploy\Backup-Data.ps1` hält den Dienst wenige Sekunden an, kopiert `data\` nach
`backup\<Datum_Uhrzeit>\` und startet ihn wieder; Sicherungen älter als 14 Tage werden gelöscht. Als
tägliche Aufgabe um 03:00:

```powershell
$aktion = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File D:\Artikelplanung\deploy\Backup-Data.ps1"
$zeit   = New-ScheduledTaskTrigger -Daily -At 03:00
Register-ScheduledTask -TaskName "Artikelplanung Backup" -Action $aktion -Trigger $zeit -User "SYSTEM" -RunLevel Highest -Force
```

Wiederherstellen: Dienst stoppen, Inhalt eines Sicherungsordners nach `data\` kopieren, Dienst starten.

---

## 9. Update einspielen

1. Auf dem Entwicklungsrechner veröffentlichen und packen (Abschnitt 3).
2. ZIP nach `D:\Artikelplanung\staging\` kopieren.
3. Auf dem Server als Administrator:

```powershell
powershell -ExecutionPolicy Bypass -File D:\Artikelplanung\deploy\Update-Server.ps1
```

Das Skript entpackt, stoppt den Dienst, legt die laufende Version nach `staging\vorher\`, setzt die
neue nach `app\` und startet den Dienst. Offene Migrationen laufen beim Start automatisch
(`Program.cs`), EF-Core-Tools sind auf dem Server nicht nötig. Zurück zur vorigen Version: Dienst
stoppen, `app\` und `staging\vorher\` tauschen, Dienst starten (eine Migration zurückzunehmen geht
damit **nicht**, dafür vorher die Sicherung aus Abschnitt 8 ziehen).

Haben sich die Skripte in `deploy\` geändert, einmal
`Copy-Item D:\Artikelplanung\app\deploy\*.ps1 D:\Artikelplanung\deploy\ -Force`.

---

## 10. Wenn etwas nicht geht

| Symptom                                                                    | Ursache und Abhilfe                                                                                                                                                                                                                                                                        |
| --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Dienst startet nicht, `Start-Service` nennt keinen Grund                   | `net start Artikelplanung` zeigt den Win32-Fehlercode im Klartext. Danach das System-Protokoll: `Get-WinEvent -LogName System -MaxEvents 100 \| Where-Object { $_.Message -like '*Artikelplanung*' } \| Format-List TimeCreated, Id, Message`.                                           |
| Dienst startet nicht, Fehler 1053 oder sofortiger Stopp                    | Ereignisprotokoll lesen (Abschnitt 7). Häufig: PFX-Pfad oder -Passwort falsch, Port 8443/8080 belegt (`netstat -ano \| findstr :8443`).                                                                                                                                                   |
| Dienst läuft kurz an, stürzt sofort ab, `Get-WinEvent` zeigt Ereignis-ID 1026 (.NET Runtime), `CryptographicException: Das angegebene Netzwerkkennwort ist falsch` | Das PFX-Passwort in der Dienstumgebung (Abschnitt 5) passt nicht zu dem Passwort, mit dem das Zertifikat exportiert wurde (Abschnitt 2) – meist ein Tippfehler bei einem der beiden. Verlässlicher als raten: Zertifikat mit einem neuen, bekannten Passwort neu exportieren (`Export-PfxCertificate … -Password $securePw`) und exakt dasselbe Passwort in die Registry-Umgebungsvariable schreiben (Abschnitt 5). So bei MSDC02 am 2026-09-22. |
| `sc.exe delete` und neu registriert, seitdem geht nichts mehr              | `sc.exe delete` löscht den kompletten Registry-Schlüssel des Dienstes, **inklusive** der `Environment`-Werte. Nach dem Neuregistrieren Abschnitt 5 erneut ausführen.                                                                                                                      |
| App nur auf dem Server erreichbar, nicht von anderen PCs                   | Firewallregeln aus Abschnitt 6 fehlen. Vom Client prüfen: `Test-NetConnection <SERVER> -Port 8443`.                                                                                                                                                                                       |
| Browser: Zertifikatswarnung                                                | `.cer` nicht in den vertrauenswürdigen Stammzertifizierungsstellen des Clients, oder Aufruf über einen anderen Namen als `<HOST>`, oder Portangabe im Link fehlt (`:8443`).                                                                                                              |
| Zertifikatswarnung **nur in Firefox**, Edge/Chrome zeigen die Seite korrekt | Firefox nutzt standardmäßig einen eigenen Zertifikatsspeicher statt des von der GPO befüllten Windows-Speichers. Schnelltest: `about:config` → `security.enterprise_roots.enabled` auf `true`. Für alle Firefox-Installationen zentral per GPO: Mozillas ADMX-Vorlagen einspielen, Richtlinie „Import Enterprise Roots" aktivieren (siehe Artefakt „Internes Root-Zertifikat per GPO verteilen"). |
| Über VPN: Seite nicht erreichbar, nicht mal `Resolve-DnsName MSDC02` funktioniert | Meist die falsche DNS-Server-IP im SSL-VPN-Profil der UTM, nicht Routing oder Firewall – vor Änderungen an der UTM erst mit `Find-NetRoute -RemoteIPAddress <IP von MSDC02>` prüfen, ob der Tunnel für das Zielnetz überhaupt die bessere (niedrigere) Metrik hat, und mit `Resolve-DnsName <HOST> -Server <IP von MSDC02>` testen, ob MSDC02 selbst korrekt antwortet (schließt Routing/Firewall/DNS-Zone als Ursache aus). So bei MSDC02 am 2026-09-22: die UTM schickte Clients noch die IP des **alten, bereits abgelösten Domänencontrollers** als DNS-Server – eine Karteileiche aus der DC-Migration, nie auf die aktuelle DC-IP aktualisiert. Fix: in den SSL-VPN-Einstellungen der Securepoint UTM die DNS-Server-IP auf den aktuellen DC (MSDC02) korrigieren. |
| Ereignisprotokoll leer                                                     | Ereignisquelle nicht angelegt (Abschnitt 1, `New-EventLog`).                                                                                                                                                                                                                              |
| Datenbank gesperrt / langsam                                               | Nicht `data\` auf ein Netzlaufwerk legen. SQLite gehört auf eine lokale Platte.                                                                                                                                                                                                           |
| `Update-Server.ps1`: „Unerwartetes Token“, im Text stehen `Ã¤` oder `â€“`  | Zeichensatz-Problem bei Windows PowerShell 5.1 ohne BOM. Die Skripte in `deploy\` liegen mit UTF-8-BOM im Repo (siehe Rechnungsablage-Doku, gleicher Fehler dort). Bei einer älteren Kopie auf dem Server einmal umspeichern, siehe dortige Anleitung.                                    |

---

## Was bewusst nicht dabei ist

- **Login/Zugriffssteuerung**: bewusst weggelassen, siehe README. Nur Zuordnung ("Eingetragen von"), kein Schutz.
- **Eigenes Dienstkonto**: nicht nötig, siehe Abschnitt 0.
- **Zugriff von außen / Reverse Proxy**: nicht vorgesehen, die App ist rein intern.
