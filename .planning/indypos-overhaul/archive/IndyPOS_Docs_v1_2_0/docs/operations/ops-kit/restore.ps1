<# 
IndyPOS StoreHub - Local PostgreSQL Restore Script (Windows)

- Restores a .dump file created by backup.ps1 (custom format)
- WARNING: This overwrites the target database.

Example:
powershell.exe -ExecutionPolicy Bypass -File restore.ps1 `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -DumpFile "C:\ProgramData\IndyPOS\backups\indypos_indypos_storehub_20260228_020000.dump" `
  -DbName "indypos_storehub" -DbAdminUser "postgres" -DbAdminPassword "..." `
  -AppUser "indypos_app"
#>

param(
  [string]$PgBin = "C:\Program Files\PostgreSQL\16\bin",
  [string]$DbHost = "127.0.0.1",
  [int]$DbPort = 5432,
  [string]$DumpFile = "",
  [string]$DbName = "indypos_storehub",
  [string]$DbAdminUser = "postgres",
  [string]$DbAdminPassword = "",
  [string]$AppUser = "indypos_app"
)

function Fail($msg) { Write-Error $msg; exit 1 }

$psql = Join-Path $PgBin "psql.exe"
$pg_restore = Join-Path $PgBin "pg_restore.exe"
if (-not (Test-Path $psql)) { Fail "psql not found at $psql. Set -PgBin." }
if (-not (Test-Path $pg_restore)) { Fail "pg_restore not found at $pg_restore. Set -PgBin." }
if (-not (Test-Path $DumpFile)) { Fail "DumpFile not found: $DumpFile" }
if ([string]::IsNullOrWhiteSpace($DbAdminPassword)) { Fail "DbAdminPassword is required." }

Write-Host "RESTORE WARNING: This will overwrite database $DbName using $DumpFile"
Write-Host "Press Ctrl+C to cancel, or wait 5 seconds..."
Start-Sleep -Seconds 5

$env:PGPASSWORD = $DbAdminPassword

& $psql -h $DbHost -p $DbPort -U $DbAdminUser -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DbName' AND pid <> pg_backend_pid();"
& $psql -h $DbHost -p $DbPort -U $DbAdminUser -d postgres -c "DROP DATABASE IF EXISTS $DbName;"
& $psql -h $DbHost -p $DbPort -U $DbAdminUser -d postgres -c "CREATE DATABASE $DbName;"

Write-Host "Restoring..."
& $pg_restore -h $DbHost -p $DbPort -U $DbAdminUser -d $DbName --clean --if-exists $DumpFile
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
  Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
  Fail "pg_restore failed with exit code $exitCode"
}

if (-not [string]::IsNullOrWhiteSpace($AppUser)) {
  & $psql -h $DbHost -p $DbPort -U $DbAdminUser -d $DbName -c "ALTER DATABASE $DbName OWNER TO $AppUser;"
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
Write-Host "✅ Restore complete."
