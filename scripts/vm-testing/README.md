# VM Smoke-Test (Stage 4)

Semi-automated installer smoke-test on a Hyper-V VM. One command from a
clean Windows snapshot to a verified IndyPOS v4 install + report.

> Why "semi"? The bootstrapper is a WinForms wizard with no silent mode (yet).
> These scripts handle every step *around* the wizard; you click through the
> wizard once per cycle inside the VM.

## Files

| File | Purpose |
|------|---------|
| `VMTestConfig.psd1` | Shared config — VM name, paths, timeouts |
| `Reset-AndInstall.ps1` | Host-side orchestrator (run this) |
| `Test-IndyPOSInstallation.ps1` | In-VM verifier (called via PSDirect) |
| `README.md` | This file |

## Prerequisites (one-time, ~30 min)

You need a Hyper-V VM named `IndyPOS-Test` with a snapshot called `Clean-Windows`.

### 1. Download Windows 11 ISO

Get the Win11 Enterprise eval ISO from
<https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-enterprise>
(90-day, no product key). Save under `C:\personal\ISOs\`.

### 2. Create the VM (run in elevated PowerShell)

Adjust `$IsoPath` to match your downloaded filename.

```powershell
$IsoPath  = 'C:\personal\ISOs\Win11_25H2_English_x64_v2.iso'  # update to your filename
$VhdxPath = 'C:\personal\VMs\IndyPOS-Test.vhdx'

New-Item -ItemType Directory -Path (Split-Path $VhdxPath) -Force | Out-Null

New-VM -Name 'IndyPOS-Test' -Generation 2 -MemoryStartupBytes 4GB `
       -NewVHDPath $VhdxPath -NewVHDSizeBytes 60GB `
       -SwitchName 'Default Switch'

Set-VMProcessor -VMName 'IndyPOS-Test' -Count 4
Set-VMMemory   -VMName 'IndyPOS-Test' -DynamicMemoryEnabled $true `
               -MinimumBytes 2GB -MaximumBytes 8GB

# Win11 needs TPM
Set-VMKeyProtector -VMName 'IndyPOS-Test' -NewLocalKeyProtector
Enable-VMTPM       -VMName 'IndyPOS-Test'

# Boot from ISO
Add-VMDvdDrive -VMName 'IndyPOS-Test' -Path $IsoPath
$dvd = Get-VMDvdDrive -VMName 'IndyPOS-Test'
Set-VMFirmware -VMName 'IndyPOS-Test' -FirstBootDevice $dvd

Start-VM 'IndyPOS-Test'
vmconnect.exe localhost 'IndyPOS-Test'
```

Install Windows manually (~15 min). At OOBE:

- Create a local Administrator account (e.g. `Pond` / strong password)
- Skip Microsoft account, network, telemetry as you like

### 3. Inside the VM, after first login (run as admin)

```powershell
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts -Value '*' -Force

# Optional: speeds tests up dramatically (Defender RT scan was the big timeout
# offender on the dev box during Stage 3).
Set-MpPreference -DisableRealtimeMonitoring $true

# .NET 10 runtime is installed by the bootstrapper, so nothing else to do here.
```

### 4. Detach the ISO and snapshot

Back on the host:

```powershell
Get-VMDvdDrive -VMName 'IndyPOS-Test' | Set-VMDvdDrive -Path $null
Checkpoint-VM -Name 'IndyPOS-Test' -SnapshotName 'Clean-Windows'
```

### 5. First credential prompt (also one-time)

The first run of `Reset-AndInstall.ps1` will pop a `Get-Credential` dialog.
Enter the VM admin username + password you set in step 2. The credential is
cached as a DPAPI-encrypted SecureString at
`%LOCALAPPDATA%\IndyPOS\vm-test-cred.xml` (readable only by your Windows user).

To rotate later: `.\Reset-AndInstall.ps1 -RecreateCredential`.

## Run a test cycle

```powershell
# From repo root
.\scripts\vm-testing\Reset-AndInstall.ps1
```

What you'll see:

```
=== IndyPOS Stage 4 — VM smoke-test ===

========================================
 1. Preflight
========================================
[ OK ] Installer: ...\publish\IndyPOS-Setup.exe (189.2 MB)
[ OK ] Verifier:  ...\Test-IndyPOSInstallation.ps1
[ OK ] VM:        IndyPOS-Test (State: Off)
[ OK ] Snapshot:  Clean-Windows

========================================
 2. Restore + Start VM
========================================
[INFO] Restoring snapshot 'Clean-Windows'...
[ OK ] Snapshot restored.
[INFO] Starting VM...
[INFO] Waiting for VM heartbeat (timeout 180s)...
[ OK ] Heartbeat: OkApplicationsHealthy

========================================
 3. PowerShell Direct readiness
========================================
[ OK ] PSDirect ready (guest hostname: DESKTOP-...)

========================================
 4. Stage installer in guest
========================================
[INFO] Copying ...\publish\IndyPOS-Setup.exe -> C:\Test\IndyPOS-Setup.exe
[ OK ] Installer staged in guest.

========================================
 5. Run wizard (manual)
========================================
[INFO] Opening vmconnect.exe — click through the wizard inside the VM.
[INFO] Inside the VM, run as admin:  C:\Test\IndyPOS-Setup.exe

========================================
 6. Wait for install-manifest.json (timeout 30 min)
========================================
[INFO] Polling guest for C:\ProgramData\IndyPOS\v4.0.0\install-manifest.json every 15s...
............................
[ OK ] install-manifest.json detected — wizard finished.

========================================
 7. In-VM verification
========================================
[INFO] Running Test-IndyPOSInstallation.ps1 inside guest...

========================================
 8. Results
========================================

[Manifest]
  [PASS] install-manifest.json present
         v4.0.0, installed 2026-05-28T...

[Prerequisites]
  [PASS] .NET 10 runtime installed
  [PASS] PostgreSQL 18 service running

[Service]
  [PASS] IndyPOS.StoreHub.v4 installed
  [PASS] Service Running
  [PASS] StartType Automatic
  [PASS] ImagePath under SystemRoot

[Filesystem]
  [PASS] SystemRoot exists
  [PASS] Config exists
  ...

[Velopack]
  [PASS] WinForms install path exists
  [PASS] IndyPOS.exe present
  [PASS] Desktop shortcut present

[Health]
  [PASS] /health/ready returns 200

========================================
 RESULT: PASS  Pass: 15  Fail: 0  Skip: 0
========================================
```

Inside the VM during step 5: open File Explorer → `C:\Test\` → right-click
`IndyPOS-Setup.exe` → Run as administrator → click through the wizard
(Store ID, App Password ×2, Start). The host script picks up automatically
when the bootstrapper writes the manifest.

## Common switches

```powershell
# Iterate without restoring (faster when you're poking at things)
.\Reset-AndInstall.ps1 -SkipRestore

# Leave VM up after success (default already leaves it up; this silences hint)
.\Reset-AndInstall.ps1 -KeepRunning

# Refresh stored creds
.\Reset-AndInstall.ps1 -RecreateCredential

# Already have vmconnect open from a previous run
.\Reset-AndInstall.ps1 -Headless

# Test a non-default installer build
.\Reset-AndInstall.ps1 -InstallerPath 'C:\some\other\IndyPOS-Setup.exe'
```

## Exit codes

| Code | Meaning |
|------|---------|
| 0    | All checks passed |
| 1    | One or more verification checks failed |
| 2    | Orchestration error (VM missing, PSDirect timeout, etc.) |

## Troubleshooting

- **`PSDirect did not become ready`** — Inside the VM, confirm
  `Enable-PSRemoting -Force` ran and the cached credential matches a local
  admin. `-RecreateCredential` to re-prompt.
- **Snapshot drift** — If you patched Windows / changed config inside the
  Clean-Windows snapshot, delete the old checkpoint and re-`Checkpoint-VM`
  after another clean boot.
- **Postgres install timeout** — Bump `InstallTimeoutMinutes` in
  `VMTestConfig.psd1`. Defender RT scan is the usual culprit; step 3's
  `Set-MpPreference -DisableRealtimeMonitoring $true` is recommended.
- **"Multiple manifests found"** — Verifier saw both `v4.0.0\` and another
  `v*\` install. Clean the VM and re-snapshot, or pass `-ManifestPath`.

## Follow-up: full automation

Adding `--silent` to the bootstrapper would let us drop the vmconnect step
and run unattended. Tracked in `.planning/indypos-overhaul/PLAN.md`.
