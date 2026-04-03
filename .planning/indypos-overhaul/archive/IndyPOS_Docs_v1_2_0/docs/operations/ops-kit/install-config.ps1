<# 
IndyPOS StoreHub - Local PostgreSQL Production Setup (Windows)
============================================================
This script configures a local PostgreSQL instance for IndyPOS StoreHub.

It supports two modes:
1) Configure an EXISTING PostgreSQL installation (most common).
2) (Optional) Initialize a NEW data directory if you have initdb available.

IMPORTANT
- Run PowerShell as Administrator.
- Recommended: Install PostgreSQL first using the official Windows installer.
- This script does NOT download installers.

What it does
- Creates folders under C:\ProgramData\IndyPOS\
- Creates an app role + database
- Sets PostgreSQL to listen on localhost only
- Hardens pg_hba for localhost
- Restarts the PostgreSQL service
#>

param(
  # Adjust to match your installed version path
  [string]$PgBin = "C:\Program Files\PostgreSQL\16\bin",
  # Name of the Windows service (check in Services.msc)
  [string]$PgServiceName = "postgresql-x64-16",

  # IndyPOS settings
  [string]$StoreId = "STORE-001",
  [string]$DbName = "indypos_storehub",
  [string]$AppUser = "indypos_app",

  # Passwords (you can pass in securely from CI/ops tooling)
  [string]$PostgresPassword = "",
  [string]$AppUserPassword = "",

  [string]$IndyRoot = "C:\ProgramData\IndyPOS",

  # Optional: If you want to initialize a new cluster (advanced)
  [switch]$InitNewCluster,
  [string]$PgData = "C:\ProgramData\IndyPOS\pgdata"
)

function Fail($msg) {
  Write-Error $msg
  exit 1
}

function Ensure-Dir($p) {
  if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null }
}

$psql = Join-Path $PgBin "psql.exe"
$pg_ctl = Join-Path $PgBin "pg_ctl.exe"
$initdb = Join-Path $PgBin "initdb.exe"

if (-not (Test-Path $psql)) { Fail "psql not found at $psql. Set -PgBin to your PostgreSQL bin folder." }

Ensure-Dir $IndyRoot
Ensure-Dir (Join-Path $IndyRoot "logs")
Ensure-Dir (Join-Path $IndyRoot "backups")
Ensure-Dir (Join-Path $IndyRoot "ops")

Write-Host "IndyPOS root: $IndyRoot"
Write-Host "Postgres bin: $PgBin"
Write-Host "Service name: $PgServiceName"

if ($InitNewCluster) {
  if (-not (Test-Path $initdb)) { Fail "initdb not found at $initdb. Cannot initialize a new cluster." }
  Ensure-Dir $PgData
  if ((Get-ChildItem -Force $PgData | Measure-Object).Count -gt 0) {
    Fail "PgData $PgData is not empty. Refusing to init a new cluster."
  }

  if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    Fail "PostgresPassword is required when initializing a new cluster."
  }

  $pwFile = Join-Path $env:TEMP "pg_pw.txt"
  Set-Content -Path $pwFile -Value $PostgresPassword -NoNewline -Encoding ASCII

  Write-Host "Initializing new PostgreSQL cluster at $PgData ..."
  & $initdb -D $PgData -U postgres --pwfile=$pwFile -A scram-sha-256 --encoding=UTF8 --locale="en-US"

  Remove-Item $pwFile -Force -ErrorAction SilentlyContinue

  Write-Host "NOTE: You must ensure your PostgreSQL service points to this PGDATA."
}

# Detect PGDATA from service PathName
$pgDataDetected = $null
try {
  $svc = Get-CimInstance Win32_Service -Filter "Name='$PgServiceName'"
  if ($svc -and $svc.PathName) {
    if ($svc.PathName -match '-D\s+"([^"]+)"') { $pgDataDetected = $Matches[1] }
    elseif ($svc.PathName -match "-D\s+([^\s]+)") { $pgDataDetected = $Matches[1] }
  }
} catch {
  Write-Warning "Could not query service path. You may need to set config paths manually."
}

if (-not $pgDataDetected) {
  Write-Warning "Could not detect PostgreSQL data directory from service. Using -PgData if it exists: $PgData"
  if (Test-Path $PgData) { $pgDataDetected = $PgData }
}

if (-not $pgDataDetected) {
  Fail "Unable to locate PostgreSQL data directory. Find your data folder (postgresql.conf) and re-run with -PgData pointing to it."
}

$conf = Join-Path $pgDataDetected "postgresql.conf"
$hba  = Join-Path $pgDataDetected "pg_hba.conf"

if (-not (Test-Path $conf)) { Fail "postgresql.conf not found at $conf" }
if (-not (Test-Path $hba))  { Fail "pg_hba.conf not found at $hba" }

Write-Host "Detected PGDATA: $pgDataDetected"
Write-Host "Config: $conf"
Write-Host "HBA:    $hba"

# Edit postgresql.conf to localhost only
$content = Get-Content $conf -Raw
if ($content -match "^\s*listen_addresses\s*=") {
  $content = [regex]::Replace($content, "^\s*listen_addresses\s*=.*$", "listen_addresses = '127.0.0.1'      # IndyPOS: local-only", "Multiline")
} else {
  $content += "`r`nlisten_addresses = '127.0.0.1'      # IndyPOS: local-only`r`n"
}

if ($content -match "^\s*password_encryption\s*=") {
  $content = [regex]::Replace($content, "^\s*password_encryption\s*=.*$", "password_encryption = 'scram-sha-256'  # IndyPOS", "Multiline")
} else {
  $content += "`r`npassword_encryption = 'scram-sha-256'  # IndyPOS`r`n"
}

Set-Content -Path $conf -Value $content -Encoding UTF8

# Prepend localhost-only rules to pg_hba.conf
$hbaLines = Get-Content $hba
$header = @(
  "# IndyPOS local-only rules (added $(Get-Date -Format s))",
  "local   all             all                                     scram-sha-256",
  "host    all             all             127.0.0.1/32            scram-sha-256",
  "host    all             all             ::1/128                 scram-sha-256",
  ""
)
$filtered = @()
$skip = $false
foreach ($l in $hbaLines) {
  if ($l -like "# IndyPOS local-only rules*") { $skip = $true; continue }
  if ($skip -and [string]::IsNullOrWhiteSpace($l)) { $skip = $false; continue }
  if (-not $skip) { $filtered += $l }
}
Set-Content -Path $hba -Value ($header + $filtered) -Encoding UTF8

Write-Host "Restarting service $PgServiceName ..."
Restart-Service -Name $PgServiceName -Force

# Create app user + db (idempotent)
if ([string]::IsNullOrWhiteSpace($AppUserPassword)) {
  Write-Warning "AppUserPassword not provided. Skipping DB/user creation."
  exit 0
}

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
  Write-Warning "PostgresPassword not provided. Assuming psql can connect as postgres without PGPASSWORD."
} else {
  $env:PGPASSWORD = $PostgresPassword
}

$createSql = @"
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$AppUser') THEN
    CREATE ROLE $AppUser LOGIN PASSWORD '$AppUserPassword';
  END IF;
END \$\$;

DO \$\$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = '$DbName') THEN
    CREATE DATABASE $DbName OWNER $AppUser;
  END IF;
END \$\$;

\\connect $DbName

CREATE SCHEMA IF NOT EXISTS indypos AUTHORIZATION $AppUser;
GRANT ALL ON SCHEMA indypos TO $AppUser;
"@

$createSql | & $psql -U postgres -h 127.0.0.1 -d postgres

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

Write-Host "`n✅ Local PostgreSQL configured for IndyPOS StoreHub."
Write-Host "StoreHub connection string (example):"
Write-Host "  Host=127.0.0.1;Port=5432;Database=$DbName;Username=$AppUser;Password=***;"
