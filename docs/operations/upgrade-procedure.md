# IndyPOS - In-Place Upgrade Procedure

> Authoritative procedure for upgrading a store that is **already running IndyPOS v4**.

## When to use this

| Situation | Use |
|-----------|-----|
| The store already runs IndyPOS v4 and you want a newer build | **This document** |
| A brand-new machine with no IndyPOS on it | [Store Installation Guide](store-installation-guide.md) |
| **The store still runs v3.7.0** | [Store Installation Guide](store-installation-guide.md) - v4 installs *alongside* v3.7.0, so this is a fresh install, not an upgrade. The installer detects that correctly and takes the fresh path |
| Manual binary-swap / migration mechanics (background reading) | [Update Procedure](update-procedure.md) |

The installer detects which case it is looking at and refuses to guess. A double-clicked
`IndyPOS-Setup.exe` on a live store now shows a message telling you to use the command
below instead of running the fresh-install wizard.

---

## The command

Run from an **elevated** command prompt on the Store Hub PC:

```
IndyPOS-Setup.exe --silent
```

That is the whole command. Notes:

- **Do not pass `--store-id`.** The upgrade adopts the store id already in the installed
  config. If you do pass it, it must match exactly, or the run refuses rather than
  rewriting store identity (which would orphan the store's sales history).
- **`--store-type` is only needed** if the store was installed before 2026-07-18 and its
  `appsettings.json` has no `Store:Type` key. The run will tell you (exit code 5) if so.
  Re-run with `--store-type GeneralHardware` or `--store-type Minimart`.
- Close the POS application on this machine first. The upgrade refuses while it is running,
  because two Velopack processes on one install root can leave the POS unlaunchable.

The POS app on **other** terminals updates itself; it does not need the installer.

---

## Exit codes

| Code | Meaning | What to do |
|------|---------|------------|
| 0 | Success | Check `POS_UPDATED` (see below), then you are done |
| 1 | Usage error | Fix the command line |
| 2 | Failed | Read `ROLLED_BACK` in the log - it says whether the store came back |
| 3 | Not elevated | Re-run as administrator |
| 4 | Timed out | Only before the backup completes - nothing was changed and the service was restarted. Read `HEALTH` |
| 5 | Unusable install | Read `REASON`, then see [Unusable-install recovery](#unusable-install-recovery) |
| 6 | Downgrade refused | This installer is older than what is installed. Use the newer installer |

---

## Reading the result

Everything the run decided is in:

```
C:\ProgramData\IndyPOS\v4\logs\install-latest.log
```

Lines beginning `INDYPOS_MARKER` are the machine-readable summary:

| Marker | Meaning |
|--------|---------|
| `MODE` | `fresh`, `upgrade` or `unusable` - which path actually ran |
| `RESULT` | `success`, `failed` or `timeout` |
| `FROM_VERSION` | Version that was installed before (or `unknown`) |
| `TO_VERSION` | Version this installer delivered |
| `SERVICE_STARTED` | Whether the StoreHub Windows service was started |
| `HEALTH` | `ok` only if `/health/ready` actually answered |
| `BACKUP_DIR` | Where the pre-upgrade dump and binaries were saved |
| `BACKUP_LOCKED` | Whether the backup was ACL-locked (see [Backups](#backups)) |
| `POS_UPDATED` | Whether the POS app package version changed |
| `ROLLED_BACK` | On a failure: whether the previous version was restored |

**`POS_UPDATED=false` alongside `RESULT=success` is not a failure.** It means the POS app
was already at this version, so Velopack had nothing to do. The installer reports it from
the package version before and after, because the Velopack exit code is 0 either way.

**A timeout is not a special case.** If the watchdog fires after the upgrade starts changing
things, the run rolls back like any other failure and reports `RESULT=failed` with
`ROLLED_BACK=true` (exit 2), not exit 4 - because what you need to know is whether the store came
back, not which clock ran out. Exit 4 means it timed out before anything was changed.

**`ROLLED_BACK=true` with `HEALTH=failed`** is the one line that needs immediate attention:
the upgrade was undone but the store did not come back. Go to
[Restoring the database dump](#restoring-the-database-dump).

---

## Unusable-install recovery

Exit code 5 means the machine is neither cleanly installable nor safely upgradeable. The
`REASON` marker names which case. **None of these are fixed by `cleanup-v4.ps1`** - that
script drops the `indypos_storehub` database, which is the store's sales history.

### A database from another IndyPOS major version

> "An IndyPOS database exists on this machine but belongs to another IndyPOS major version."

**Do not remove PostgreSQL.** It holds the store's sales history. The database name is not
version-scoped, so a newer installer can see an older store's data.

Look under `C:\ProgramData\IndyPOS\` for the `v<N>` folder that owns the existing install,
and run the installer for **that** major version instead.

### The connection string cannot be decrypted

> "The StoreHub connection string is missing, empty, or cannot be decrypted on this machine."

Restore `appsettings.json` from the newest backup:

```
C:\ProgramData\IndyPOS\v4\backups\<stamp>\StoreHub\appsettings.json
```

A `DPAPI:`-prefixed value is sealed to the machine that wrote it. If the config came from a
different PC, it can never be decrypted here - you need this machine's own backup, or a
fresh install plus a database restore.

### `Store:Id` is missing

> "Store:Id is missing from appsettings.json."

Set it to the store's real id before upgrading. The authoritative value is in the database:

```powershell
# From an elevated prompt, with PGPASSWORD set as in the restore section below.
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h 127.0.0.1 -U indypos_app `
    -d indypos_storehub -w -c 'SELECT DISTINCT store_id FROM store_user;'
```

Upgrading without it would seed a second payment-method catalogue under a fallback id and
orphan the store's history.

### `Store:Type` is missing

Stores installed before 2026-07-18 have no such key. It cannot be defaulted: the default is
the most permissive store type and would silently re-enable restricted features.

**`--store-type` does not currently fix this on an upgrade** - the run is refused before any
argument is consulted. Add the key by hand, then re-run. `appsettings.json` is ACL-locked to
Administrators, so use an elevated editor:

```
C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json
```

```json
  "store": {
    "id": "Rungrat-001",
    "type": "GeneralHardware"
  },
```

Use the store's real type (`GeneralHardware` or `Minimart`). Then:

```
IndyPOS-Setup.exe --silent
```

### The service is not registered, or points elsewhere

> "The StoreHub service is not registered, or its ImagePath does not resolve under ..."

Re-register it against the real binary, then re-run the upgrade:

```powershell
sc.exe create IndyPOS.StoreHub.v4 `
    binPath= "C:\ProgramData\IndyPOS\v4\StoreHub\IndyPOS.StoreHub.exe" `
    start= auto DisplayName= "IndyPOS StoreHub v4"
```

### A config with no manifest

> "A StoreHub configuration exists but there is no install manifest."

A previous install did not finish. Inspect `C:\ProgramData\IndyPOS\v4\StoreHub\` and decide
deliberately: if there is no data worth keeping, a clean install is correct; if the database
holds real sales, restore the config from a backup first.

---

## Restoring the database dump

**This is always a human decision.** The installer never restores the dump automatically -
rolling back binaries is safe, silently replacing a live database is not.

```powershell
# Stop the service first, then restore into the existing database.
Stop-Service IndyPOS.StoreHub.v4
$env:PGPASSWORD = '<indypos_app password from the connection string>'
& 'C:\Program Files\PostgreSQL\18\bin\pg_restore.exe' `
    --host=127.0.0.1 --port=5432 --username=indypos_app `
    --dbname=indypos_storehub --clean --if-exists --no-owner --no-password `
    'C:\ProgramData\IndyPOS\v4\backups\<stamp>\storehub.dump'
Start-Service IndyPOS.StoreHub.v4
```

Then confirm the store is actually serving:

```powershell
Invoke-RestMethod 'http://localhost:5000/health/ready'
```

---

## Backups

Every upgrade takes a backup **before** it changes anything:

```
C:\ProgramData\IndyPOS\v4\backups\<yyyyMMdd-HHmmss>\
    storehub.dump        <- pg_dump of the whole store database
    StoreHub\            <- a full copy of the binaries and config
```

- The **last 2 stamps** are kept; older ones are pruned. Each is roughly 130 MB.
- They are **ACL-locked to Administrators + LocalSystem**, because the dump contains the
  full sales history and the admin BCrypt hashes.
- `BACKUP_LOCKED=false` in the log means that lock failed. Fix the ACL before leaving the
  store - by default `C:\ProgramData` grants read to `BUILTIN\Users`.

---

## Known limitations

1. **The POS app is installed per-user.** The installer updates the POS for the account it
   runs under. A cashier who logs in with a different Windows account keeps their own copy
   until it self-updates.
2. **The POS app self-updates independently** of StoreHub. A terminal can briefly run a
   newer or older POS than the machine you upgraded.
3. **One machine per run.** There is no fan-out; visit each Store Hub PC.
4. **An upgrade does not create `StoreConfiguration.json`.** A store missing it was missing
   it before the upgrade too - see the [Store Installation Guide](store-installation-guide.md).
5. **Schema is never rolled back.** A failed upgrade restores binaries and config only,
   which is why every migration must be forward-only (see `CLAUDE.md`).

---

## Change Log

| Date | Change |
|------|--------|
| 2026-07-29 | Initial in-place upgrade procedure |
