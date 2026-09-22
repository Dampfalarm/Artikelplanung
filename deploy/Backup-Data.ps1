<#
.SYNOPSIS
  Sichert das Datenverzeichnis der Artikelplanung (SQLite-Datenbank).

.DESCRIPTION
  Hält den Dienst kurz an, damit die SQLite-Datei konsistent kopiert wird, kopiert
  D:\Artikelplanung\data nach D:\Artikelplanung\backup\<Datum_Uhrzeit> und startet den
  Dienst wieder. Sicherungen, die älter als -KeepDays sind, werden gelöscht.
  Gedacht für eine tägliche Aufgabe nachts (docs/deployment.md, Abschnitt "Backup").
#>
param(
    [string]$Root = 'D:\Artikelplanung',
    [string]$ServiceName = 'Artikelplanung',
    [int]$KeepDays = 14
)

$ErrorActionPreference = 'Stop'
$data   = Join-Path $Root 'data'
$backup = Join-Path $Root 'backup'
$target = Join-Path $backup (Get-Date -Format 'yyyy-MM-dd_HHmm')

if (-not (Test-Path $data)) { throw "Datenverzeichnis $data nicht gefunden." }
New-Item -ItemType Directory -Force -Path $target | Out-Null

$svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
$wasRunning = $svc -and $svc.Status -eq 'Running'
try {
    if ($wasRunning) {
        Stop-Service $ServiceName
        $svc.WaitForStatus('Stopped', (New-TimeSpan -Seconds 60))
    }
    Copy-Item (Join-Path $data '*') $target -Recurse -Force
    Write-Host "Gesichert nach $target"
}
finally {
    if ($wasRunning) { Start-Service $ServiceName }
}

Get-ChildItem $backup -Directory |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } |
    ForEach-Object { Write-Host "Entferne alte Sicherung $($_.FullName)"; Remove-Item $_.FullName -Recurse -Force }
