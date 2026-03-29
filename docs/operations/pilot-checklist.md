# IndyPOS StoreHub - Pilot Deployment Checklist

## Overview
This checklist is for deploying IndyPOS StoreHub at the pilot store (Store 1).

**Target Environment:**
- Windows 10/11 PC (Store Hub)
- PostgreSQL 16
- 1-2 POS terminals connecting via LAN

---

## Pre-Deployment Checklist

### Hardware & Network
- [ ] Store Hub PC meets minimum specs (4GB RAM, 50GB disk)
- [ ] Static IP assigned to Store Hub PC
- [ ] POS terminals can ping Store Hub IP
- [ ] Internet connection available (for cloud sync)
- [ ] UPS connected for Store Hub PC

### Software Prerequisites
- [ ] Windows 10/11 updated
- [ ] .NET 10 Runtime installed
- [ ] PostgreSQL 16 installed (official Windows installer)
- [ ] Docker Desktop installed (optional, for local dev testing)

### Backup Preparation
- [ ] SQLite database backup taken: `C:\ProgramData\IndyPOS\db\Store.db`
- [ ] Backup stored in separate location (USB/network drive)
- [ ] Current day's sales recorded (for verification)

---

## Deployment Steps

### Step 1: Configure PostgreSQL
```powershell
# Run as Administrator
cd C:\ProgramData\IndyPOS\ops

.\install-config.ps1 `
  -PgBin "C:\Program Files\PostgreSQL\16\bin" `
  -StoreId "STORE-001" `
  -DbName "indypos_storehub" `
  -AppUser "indypos_app" `
  -PostgresPassword "<POSTGRES_PASSWORD>" `
  -AppUserPassword "<APP_PASSWORD>"
```

**Verification:**
- [ ] Script completes without errors
- [ ] PostgreSQL service running: `Get-Service postgresql*`
- [ ] Can connect: `psql -U indypos_app -d indypos_storehub -h 127.0.0.1`

### Step 2: Install StoreHub Service
```powershell
# Copy StoreHub binaries to install location
Copy-Item -Path ".\publish\*" -Destination "C:\Program Files\IndyPOS\StoreHub" -Recurse

# Create Windows service
New-Service -Name "IndyPOS.StoreHub" `
  -BinaryPathName "C:\Program Files\IndyPOS\StoreHub\IndyPOS.StoreHub.exe" `
  -DisplayName "IndyPOS StoreHub" `
  -StartupType Automatic `
  -Description "IndyPOS local API service"

# Start service
Start-Service -Name "IndyPOS.StoreHub"
```

**Verification:**
- [ ] Service running: `Get-Service IndyPOS.StoreHub`
- [ ] Health check passes (see Step 3)

### Step 3: Verify Health Endpoints
```powershell
# Basic health
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET

# Ready check (includes DB connection)
Invoke-RestMethod -Uri "http://localhost:5000/health/ready" -Method GET
```

**Expected Response:**
```json
{"status": "Healthy"}
```

**Verification:**
- [ ] `/health` returns 200 OK
- [ ] `/health/ready` returns 200 OK

### Step 4: Migrate Data from SQLite
```powershell
# Run migration tool (one-time)
cd "C:\Program Files\IndyPOS\StoreHub"
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --store-id "STORE-001"
```

**Verification:**
- [ ] Migration completes without errors
- [ ] Product count matches SQLite
- [ ] User count matches SQLite
- [ ] Invoice count matches SQLite

### Step 5: Configure Backup Schedule
```powershell
# Create scheduled task for backups every 4 hours
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument '-ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1" -PgBin "C:\Program Files\PostgreSQL\16\bin" -DbName "indypos_storehub" -DbUser "indypos_app" -DbPassword "<APP_PASSWORD>" -BackupDir "C:\ProgramData\IndyPOS\backups" -Retention 60'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 4)
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount

Register-ScheduledTask -TaskName "IndyPOS-Backup" -Action $action -Trigger $trigger -Principal $principal
```

**Verification:**
- [ ] Scheduled task created: `Get-ScheduledTask IndyPOS-Backup`
- [ ] Manual backup test: `.\backup.ps1 ...` creates `.dump` file
- [ ] Backup file size > 0

### Step 6: Configure WinForms Client
Update `appsettings.json` on POS terminals:
```json
{
  "StoreHub": {
    "BaseUrl": "http://<STOREHUB_IP>:5000",
    "Enabled": true
  }
}
```

**Verification:**
- [ ] WinForms app connects to StoreHub
- [ ] Login works with migrated credentials
- [ ] Product search returns results

### Step 7: Test Complete Sale Flow
1. [ ] Search for a product by barcode
2. [ ] Add product to cart
3. [ ] Complete cash payment
4. [ ] Verify invoice created
5. [ ] Verify inventory deducted
6. [ ] Print receipt (if applicable)

---

## Post-Deployment Verification

### Data Integrity Checks
```powershell
# Run smoke test
.\smoke-test.ps1
```

- [ ] All smoke tests pass
- [ ] Product count matches pre-migration
- [ ] Today's sales total matches (if any sales before cutover)

### Sync Status
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/sync/status" `
  -Headers @{Authorization = "Bearer <TOKEN>"}
```

- [ ] Sync status is "synced" or "pending" (not "error")
- [ ] If pending, events are draining

### Performance Check
- [ ] Product search responds < 500ms
- [ ] Sale completion responds < 1s
- [ ] No timeout errors in logs

---

## Go-Live Approval

| Check | Owner | Sign-off |
|-------|-------|----------|
| Hardware ready | IT | [ ] |
| PostgreSQL configured | IT | [ ] |
| StoreHub service running | IT | [ ] |
| Data migration verified | IT | [ ] |
| Backup schedule active | IT | [ ] |
| Test sale completed | Store Manager | [ ] |
| Staff trained | Store Manager | [ ] |

**Go-Live Decision:** [ ] APPROVED / [ ] BLOCKED

**Notes:**
_____________________________________________

---

## Emergency Contacts

| Role | Name | Phone |
|------|------|-------|
| IT Support | | |
| Store Manager | | |
| Developer (escalation) | | |

---

## Rollback Trigger Conditions
Initiate rollback if ANY of these occur:
1. StoreHub service crashes repeatedly (> 3 times in 1 hour)
2. Sales cannot be completed
3. Data loss detected
4. Performance degradation > 10x normal

See: `rollback-plan.md`
