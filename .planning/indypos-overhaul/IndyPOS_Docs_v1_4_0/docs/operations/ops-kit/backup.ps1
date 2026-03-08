<# 
IndyPOS StoreHub - Local PostgreSQL Backup Script (Windows)

- Runs pg_dump to a timestamped file
- Keeps only the most recent N backups
- Optional: copy to offsite folder
- Designed for Windows Task Scheduler
#>

param(
  [string]$PgBin = "C:\Program Files\PostgreSQL\16\bin",
  [string]$DbHost = "127.0.0.1",
  [int]$DbPort = 5432,
  [string]$DbName = "indypos_storehub",
  [string]$DbUser = "indypos_app",
  [string]$DbPassword = "",
  [string]$BackupDir = "C:\ProgramData\IndyPOS\backups",
  [int]$Retention = 60,
  [string]$OffsiteDir = ""
)

function Fail($msg) { Write-Error $msg; exit 1 }
function Ensure-Dir($p) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null } }

$pg_dump = Join-Path $PgBin "pg_dump.exe"
if (-not (Test-Path $pg_dump)) { Fail "pg_dump not found at $pg_dump. Set -PgBin." }

Ensure-Dir $BackupDir
if (-not [string]::IsNullOrWhiteSpace($OffsiteDir)) { Ensure-Dir $OffsiteDir }

$ts = Get-Date -Format "yyyyMMdd_HHmmss"
$file = Join-Path $BackupDir "indypos_${DbName}_${ts}.dump"

$env:PGPASSWORD = $DbPassword
& $pg_dump -h $DbHost -p $DbPort -U $DbUser -F c -Z 6 -f $file $DbName
$exitCode = $LASTEXITCODE
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
if ($exitCode -ne 0) { Fail "pg_dump failed with exit code $exitCode" }

$backups = Get-ChildItem $BackupDir -Filter "indypos_${DbName}_*.dump" | Sort-Object LastWriteTime -Descending
if ($backups.Count -gt $Retention) {
  $toDelete = $backups | Select-Object -Skip $Retention
  foreach ($b in $toDelete) { Remove-Item $b.FullName -Force -ErrorAction SilentlyContinue }
}

if (-not [string]::IsNullOrWhiteSpace($OffsiteDir)) {
  $dest = Join-Path $OffsiteDir (Split-Path $file -Leaf)
  Copy-Item $file $dest -Force
}

Write-Host "Backup complete."
