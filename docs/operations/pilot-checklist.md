# IndyPOS StoreHub - Pilot Deployment Checklist

## Overview
This checklist is for deploying IndyPOS StoreHub at the pilot store (Store 1).

**Target Environment:**
- Windows 10/11 PC (Store Hub)
- PostgreSQL 18
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
- [ ] PostgreSQL 18 installed (official Windows installer)
- [ ] Docker Desktop installed (optional, for local dev testing)

### Backup Preparation
- [ ] SQLite database backup taken: `C:\ProgramData\IndyPOS\db\Store.db`
- [ ] Backup stored in separate location (USB/network drive)
- [ ] Current day's sales recorded (for verification)

---

## Deployment Steps

### Step 1: Configure PostgreSQL

> ⚠️ **Deprecated.** `install-config.ps1` has been removed; the automated installer
> (`IndyPOS-Setup.exe`) is the supported path and handles PostgreSQL + DPAPI-protected
> secrets. The manual command below is retained for reference only.

```powershell
# Run as Administrator
cd C:\ProgramData\IndyPOS\ops

.\install-config.ps1 `
  -PgBin "C:\Program Files\PostgreSQL\18\bin" `
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

# Step 4a: Dry run first to validate data
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=<APP_PASSWORD>" `
  --store-id "STORE-001" `
  --dry-run

# Step 4b: Execute actual migration
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=<APP_PASSWORD>" `
  --store-id "STORE-001"

# Step 4c: Verify migration with verification tool
.\IndyPOS.MigrationTool.exe verify `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=<APP_PASSWORD>" `
  --store-id "STORE-001"
```

**Optional: Cloud Sync After Migration**
```powershell
# If cloud sync is configured, add OAuth2 credentials
.\IndyPOS.MigrationTool.exe `
  --sqlite "C:\ProgramData\IndyPOS\db\Store.db" `
  --postgres "Host=127.0.0.1;Database=indypos_storehub;Username=indypos_app;Password=<APP_PASSWORD>" `
  --store-id "STORE-001" `
  --cloud-api "https://cloud.indypos.app" `
  --client-id "<CLIENT_ID>" `
  --client-secret "<CLIENT_SECRET>"
```

#### ⛔ Read this before running Step 4b

**The tool now refuses a second run against a store it has already migrated.** It reports
`Migration REFUSED … already has N migrated invoice(s)`, aborts, and writes nothing — verified on
GeneralHardware, whose 139,680 invoices and ฿12,414,071 were left untouched by the attempt.

That guard exists because a re-run used to silently **double** the store's recorded turnover: a real
test took one store from 15 invoices / ฿1,056 to 30 / ฿2,112. Re-running is therefore no longer
destructive, but it is still never the fix — see the table below for what each outcome means.

⚠️ The guard recognises a previous migration by the legacy ids it wrote. A store migrated by a build
older than this one has no legacy ids, so it would **not** be recognised. No store is live on v4 yet,
so today that is theoretical; migrate into an empty database and it cannot arise.

So a non-zero exit code from Step 4b does **not** mean "try again". The tool tells you which of four
cases you are in, and only one of them is safe to re-run:

| What it prints | Exit | Was anything written? | What to do |
|---|---|---|---|
| `✓ Migration completed successfully!` | 0 | Yes, everything | Go to Step 4c |
| `✓ Migration completed with warnings` | 0 | Yes, everything | **Read the warnings**, then Step 4c. Nothing was refused, but something is worth knowing — most often a product category the v4 catalogue does not have |
| `✗ Migration completed with errors` | 1 | **Yes — the rows that succeeded ARE saved** | **Do not re-run** — it will be refused anyway. Fix the causes in the *legacy* database, or settle them by hand. The banner says this too |
| `✗ Migration ABORTED - nothing was written` | 1 | **No, nothing at all** | Safe to fix and re-run. The database is untouched. **`AlreadyMigrated` is this case too** — it means this store was migrated before, so re-running cannot help; `verify` the existing migration instead |

Only the **ABORTED** case may be re-run. It is the only one that wrote nothing.

**Do Step 4a (dry run) first and read its output properly** — it reports the same problems as the real
run while writing nothing to the database, so every surprise below is one you can meet *before*
committing. It takes seconds.

#### Expected time and resources for Step 4b

| Store size | Time | Peak memory |
|---|---|---|
| Small (hundreds of invoices) | seconds | < 500 MB |
| Large (~140,000 invoices, ~326,000 lines) | **about 1 minute** | **up to ~2.8 GB** |

Measured on the largest real store. **If a migration runs for many minutes, something is wrong** —
stop and investigate rather than waiting. Make sure the machine has the memory free; the whole
migration is held in memory and committed once, so that it is all-or-nothing.

#### Step 4d: collect the clamped-stock report

If any product had **negative** stock in the legacy database, the tool migrates it as **0** and writes
`clamped-stock-<timestamp>.csv` beside the legacy database, naming every product. On the largest real
store that is **952 products**.

- [ ] If the run mentions clamped stock, **keep that CSV** and give it to the shop for a physical
      recount. Those quantities are not recoverable from anywhere else.

**Verification:**
- [ ] Dry run (4a) completed and its output was **read**, not just glanced at
- [ ] Step 4b printed one of the four banners above, and you took the matching action
- [ ] **The migration was NOT run twice**
- [ ] Verification tool (4c) shows every row ✓ — **or** the only ✗ rows are ones you have consciously
      accepted, with a note in the sign-off table saying which and why
- [ ] Product count matches: SQLite → PostgreSQL
- [ ] User count matches: SQLite → PostgreSQL
- [ ] Invoice count matches: SQLite → PostgreSQL
- [ ] Revenue totals match (within ±฿1.00)
- [ ] `Stock (units)` row matches
- [ ] `Categories` row matches — every product carries a real catalogue code, not a legacy id
- [ ] `Barcode keys` row reads 0
- [ ] Clamped-stock CSV collected if the run produced one

#### What each `verify` row means, and which ✗ you may have to accept

`verify` reports per-entity counts plus four value checks. Most ✗ rows mean the migration went wrong.
**Two do not** — they are properties of the store's own legacy data. One of those is fixable at
source, the other is not fixable at all:

| Row | A ✗ here means |
|---|---|
| `Stock (units)` | Legacy `QuantityInStock` does not equal the sum of migrated movements. A real problem |
| `Categories` | A product's category is not the catalogue code its legacy category maps to. A real problem |
| `Barcode keys` | Two products share the first 50 characters of their barcode, so they cannot both migrate. **Fix in the legacy database** — shorten one |
| `Payments (no invoice)` | Payments reference an invoice that no longer exists, so they cannot be attached to anything. **Not fixable by the tool.** Settle the amount by hand. Re-running will not recover it |

⚠️ **One real store is expected to fail permanently on `Payments (no invoice)`.** GeneralHardware has
two such payments totalling ฿1,000. Every other row is green. Do not chase this as a migration bug and
do **not** re-run to try to clear it — record it in the sign-off table as accepted, with the amount.

### Step 5: Configure Backup Schedule
```powershell
# Create scheduled task for backups every 4 hours
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument '-ExecutionPolicy Bypass -File "C:\ProgramData\IndyPOS\ops\backup.ps1" -PgBin "C:\Program Files\PostgreSQL\18\bin" -DbName "indypos_storehub" -DbUser "indypos_app" -DbPassword "<APP_PASSWORD>" -BackupDir "C:\ProgramData\IndyPOS\backups" -Retention 60'

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
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
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
| Migration run **once only** | IT | [ ] |
| Clamped-stock CSV handed to the shop (if produced) | IT | [ ] |
| Accepted `verify` exceptions recorded below | IT | [ ] |
| Backup schedule active | IT | [ ] |
| Test sale completed | Store Manager | [ ] |
| Staff trained | Store Manager | [ ] |

**Accepted `verify` exceptions.** Any ✗ row you are going live with, and why. Leave blank if every
row was ✓. A store with an entry here is going live with data that will never reach v4, so the amount
matters:

| `verify` row | Count | Amount at stake | Why accepted |
|---|---|---|---|
| | | | |

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
