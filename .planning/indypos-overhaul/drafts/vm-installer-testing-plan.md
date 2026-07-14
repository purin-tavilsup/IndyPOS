# VM Installer Testing Plan

> Automate IndyPOS installer testing using Hyper-V VMs

## Overview

Two phases of automation:
1. **Quick Win**: Script-driven testing with pre-installed Windows VM
2. **Full Automation**: Unattended Windows install + complete test pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         VM INSTALLER TESTING                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   QUICK WIN (Phase 1)                  FULL AUTOMATION (Phase 2)            │
│   ─────────────────────                ─────────────────────────            │
│   Manual: Install Windows once         Auto: Unattended Windows install     │
│   Manual: Create "Clean" snapshot      Auto: Create VM from scratch         │
│   Auto: Restore snapshot               Auto: Everything                     │
│   Auto: Copy installer                                                      │
│   Auto: Run installer                                                       │
│   Auto: Verify installation                                                 │
│   Auto: Report results                                                      │
│                                                                             │
│   Time: 1-2 hours                      Time: 4-6 hours                      │
│   Reusable: Yes (same VM)              Reusable: Yes (any machine)          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Quick Win

### Goal
One-command installer testing using existing Windows VM snapshot.

### Prerequisites (Manual, One-Time)
1. Windows 11 VM created in Hyper-V
2. Windows installed and configured
3. Snapshot named `Clean-Windows` created
4. Admin credentials known

### Scripts to Create

#### 1.1 `scripts/vm-testing/Initialize-TestVM.ps1`
One-time setup script to create the base VM.

```powershell
# Creates VM, requires manual Windows installation afterward
param(
    [string]$VMName = "IndyPOS-Test",
    [string]$ISOPath,  # Path to Windows ISO
    [int]$MemoryGB = 4,
    [int]$DiskGB = 60,
    [int]$Processors = 4
)

# Creates:
# - New Gen2 VM
# - Attaches ISO
# - Configures memory, CPU, network
# - Outputs: "Now install Windows manually, then run New-CleanSnapshot.ps1"
```

#### 1.2 `scripts/vm-testing/New-CleanSnapshot.ps1`
Creates the "Clean-Windows" snapshot after manual Windows setup.

```powershell
param(
    [string]$VMName = "IndyPOS-Test",
    [string]$SnapshotName = "Clean-Windows"
)

# Creates:
# - Checkpoint named "Clean-Windows"
# - This is the restore point for all future tests
```

#### 1.3 `scripts/vm-testing/Test-IndyPOSInstaller.ps1`
Main test script - run this for each installer test.

```powershell
param(
    [string]$VMName = "IndyPOS-Test",
    [string]$InstallerPath,  # Path to IndyPOS-Setup.exe
    [string]$SnapshotName = "Clean-Windows",
    [switch]$KeepRunning,    # Don't shut down after test
    [switch]$SkipRestore     # Don't restore snapshot (test on current state)
)

# Steps:
# 1. Restore VM to clean snapshot
# 2. Start VM
# 3. Wait for VM to be ready
# 4. Copy installer to VM
# 5. Run installer (silent mode)
# 6. Run verification checks
# 7. Output results
# 8. Shut down (unless -KeepRunning)
```

#### 1.4 `scripts/vm-testing/Test-IndyPOSInstallation.ps1`
Verification script that runs INSIDE the VM.

```powershell
# Runs inside VM via PowerShell Direct
# Returns structured test results

# Checks:
# - IndyPOS files exist
# - PostgreSQL service running
# - IndyPOS.exe launches
# - StoreHub API responds (health check)
# - Can create test sale (optional)
```

### Test Flow (Quick Win)

```
┌─────────────────────────────────────────────────────────────────┐
│  Developer runs: .\Test-IndyPOSInstaller.ps1 -InstallerPath ... │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  1. Restore-VMSnapshot "Clean-Windows"                          │
│     └─ VM returns to pristine state                             │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  2. Start-VM + Wait for ready                                   │
│     └─ Wait for heartbeat + WinRM                               │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  3. Copy-VMFile (installer → VM)                                │
│     └─ C:\Test\IndyPOS-Setup.exe                                │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  4. Invoke-Command -VMName (run installer)                      │
│     └─ Start-Process IndyPOS-Setup.exe /silent -Wait            │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  5. Invoke-Command -VMName (verify)                             │
│     └─ Run Test-IndyPOSInstallation.ps1 inside VM               │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  6. Output Results                                              │
│     ┌─────────────────────────────────────────────────────────┐ │
│     │ IndyPOS Installer Test Results                          │ │
│     │ ─────────────────────────────────────────────────────── │ │
│     │ ✅ Files installed correctly                            │ │
│     │ ✅ PostgreSQL service running                           │ │
│     │ ✅ IndyPOS.exe launches                                 │ │
│     │ ✅ StoreHub health check passed                         │ │
│     │ ─────────────────────────────────────────────────────── │ │
│     │ RESULT: PASSED                                          │ │
│     └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Verification Checks

| Check | Command | Pass Criteria |
|-------|---------|---------------|
| Files exist | `Test-Path "C:\Program Files\IndyPOS\*"` | IndyPOS.exe exists |
| PostgreSQL installed | `Get-Service postgresql*` | Service exists |
| PostgreSQL running | `(Get-Service postgresql*).Status` | Status = Running |
| App launches | `Start-Process IndyPOS.exe; Start-Sleep 5` | Process running |
| Health check | `Invoke-WebRequest http://localhost:5012/health` | 200 OK |

---

## Phase 2: Full Automation

### Goal
Create VM from scratch with unattended Windows install - zero manual steps.

### Additional Scripts

#### 2.1 `scripts/vm-testing/autounattend.xml`
Windows answer file for unattended installation.

```xml
<?xml version="1.0" encoding="utf-8"?>
<unattend xmlns="urn:schemas-microsoft-com:unattend">
  <!-- Configures:
    - Language/locale
    - Disk partitioning (UEFI)
    - Admin account (known password)
    - Skip OOBE screens
    - Enable PowerShell remoting
    - Disable Windows Defender (for faster testing)
  -->
</unattend>
```

#### 2.2 `scripts/vm-testing/New-TestVMFromScratch.ps1`
Creates VM and installs Windows automatically.

```powershell
param(
    [string]$VMName = "IndyPOS-Test",
    [string]$ISOPath,           # Windows ISO
    [string]$AnswerFilePath,    # autounattend.xml
    [int]$TimeoutMinutes = 30   # Wait for Windows install
)

# Steps:
# 1. Create new VM
# 2. Create custom ISO with autounattend.xml injected
# 3. Attach ISO and start VM
# 4. Wait for Windows installation to complete
# 5. Wait for first boot + configuration
# 6. Create "Clean-Windows" snapshot
# 7. Output: "VM ready for testing"
```

#### 2.3 `scripts/vm-testing/New-CustomWindowsISO.ps1`
Injects answer file into Windows ISO.

```powershell
param(
    [string]$SourceISO,
    [string]$AnswerFile,
    [string]$OutputISO
)

# Uses oscdimg.exe (from Windows ADK) to create custom ISO
# with autounattend.xml at root - triggers unattended install
```

### Full Automation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Developer runs: .\New-TestVMFromScratch.ps1 -ISOPath ...       │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  1. Create custom ISO (inject autounattend.xml)                 │
│     └─ Windows will auto-install with predefined settings       │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  2. Create VM + attach custom ISO                               │
│     └─ Gen2, 4GB RAM, 60GB disk, Default Switch                 │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  3. Start VM                                                    │
│     └─ Windows installs automatically (~15-20 min)              │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  4. Wait for installation complete                              │
│     └─ Poll for heartbeat + successful login                    │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  5. Post-install configuration                                  │
│     └─ Enable PowerShell Direct, disable Defender, etc.         │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  6. Create "Clean-Windows" snapshot                             │
│     └─ Ready for installer testing                              │
└─────────────────────────────────┬───────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│  7. Run Test-IndyPOSInstaller.ps1 (same as Phase 1)             │
└─────────────────────────────────────────────────────────────────┘
```

### Full Automation Prerequisites

| Requirement | Purpose | How to Get |
|-------------|---------|------------|
| Windows 11 ISO | Base OS | Download from Microsoft |
| Windows ADK | oscdimg.exe for ISO creation | `winget install Microsoft.WindowsADK` |
| Product key (optional) | Activate Windows | Use eval key or skip |

---

## File Structure

```
scripts/
└── vm-testing/
    ├── README.md                      # Quick start guide
    │
    │   # Phase 1: Quick Win
    ├── Initialize-TestVM.ps1          # Create base VM (manual Windows install)
    ├── New-CleanSnapshot.ps1          # Create clean checkpoint
    ├── Test-IndyPOSInstaller.ps1      # Main test runner
    ├── Test-IndyPOSInstallation.ps1   # Verification (runs in VM)
    │
    │   # Phase 2: Full Automation
    ├── autounattend.xml               # Windows answer file
    ├── New-CustomWindowsISO.ps1       # Inject answer file into ISO
    ├── New-TestVMFromScratch.ps1      # Full automated VM creation
    │
    │   # Shared
    └── VMTestConfig.psd1              # Configuration (VM name, paths, credentials)
```

---

## Configuration File

`scripts/vm-testing/VMTestConfig.psd1`:

```powershell
@{
    # VM Settings
    VMName = "IndyPOS-Test"
    MemoryGB = 4
    DiskGB = 60
    Processors = 4
    SwitchName = "Default Switch"

    # Paths
    VMStoragePath = "C:\VMs"
    WindowsISOPath = "C:\ISOs\Win11.iso"

    # Snapshots
    CleanSnapshotName = "Clean-Windows"
    InstalledSnapshotName = "IndyPOS-Installed"

    # Credentials (for PowerShell Direct)
    VMAdminUser = "Administrator"
    # Password stored in Windows Credential Manager: "IndyPOS-TestVM"

    # Timeouts
    VMStartTimeoutSeconds = 120
    InstallerTimeoutSeconds = 300
    WindowsInstallTimeoutMinutes = 30

    # Test Settings
    InstallerSilentArgs = "/silent /norestart"
    HealthCheckUrl = "http://localhost:5012/health"
    HealthCheckTimeoutSeconds = 60
}
```

---

## Implementation Tasks

### Phase 1: Quick Win (1-2 hours)

| Task | Est. | Priority |
|------|------|----------|
| P1.1: Create `Initialize-TestVM.ps1` | 20 min | High |
| P1.2: Create `New-CleanSnapshot.ps1` | 10 min | High |
| P1.3: Create `Test-IndyPOSInstaller.ps1` | 45 min | High |
| P1.4: Create `Test-IndyPOSInstallation.ps1` | 30 min | High |
| P1.5: Create `VMTestConfig.psd1` | 10 min | High |
| P1.6: Create `README.md` | 15 min | Medium |
| P1.7: Manual test & debug | 30 min | High |

### Phase 2: Full Automation (4-6 hours)

| Task | Est. | Priority |
|------|------|----------|
| P2.1: Create `autounattend.xml` | 1 hour | Medium |
| P2.2: Create `New-CustomWindowsISO.ps1` | 45 min | Medium |
| P2.3: Create `New-TestVMFromScratch.ps1` | 1 hour | Medium |
| P2.4: Install Windows ADK | 15 min | Medium |
| P2.5: Test full automation flow | 1 hour | Medium |
| P2.6: Debug & edge cases | 1 hour | Medium |

---

## Usage Examples

### Phase 1: Quick Win

```powershell
# One-time setup (manual Windows install required)
.\Initialize-TestVM.ps1 -ISOPath "C:\ISOs\Win11.iso"
# ... install Windows manually ...
.\New-CleanSnapshot.ps1

# Run tests (repeatable)
.\Test-IndyPOSInstaller.ps1 -InstallerPath ".\releases\IndyPOS-Setup.exe"

# Run test but keep VM running for debugging
.\Test-IndyPOSInstaller.ps1 -InstallerPath ".\releases\IndyPOS-Setup.exe" -KeepRunning

# Test upgrade scenario
.\Test-IndyPOSInstaller.ps1 -InstallerPath ".\releases\v1.0\IndyPOS-Setup.exe"
.\Test-IndyPOSInstaller.ps1 -InstallerPath ".\releases\v1.1\IndyPOS-Setup.exe" -SkipRestore
```

### Phase 2: Full Automation

```powershell
# Create VM from scratch (no manual steps)
.\New-TestVMFromScratch.ps1 -ISOPath "C:\ISOs\Win11.iso"

# Then run tests as usual
.\Test-IndyPOSInstaller.ps1 -InstallerPath ".\releases\IndyPOS-Setup.exe"
```

---

## Success Criteria

### Phase 1 Complete When:
- [ ] Can create test VM with one command
- [ ] Can run installer test with one command
- [ ] Test results clearly show pass/fail
- [ ] Can restore and re-test in < 5 minutes

### Phase 2 Complete When:
- [ ] Can create VM + install Windows with zero manual steps
- [ ] Full test cycle runs unattended
- [ ] Works on any Windows machine with Hyper-V

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Windows activation | Nag screens | Use eval version (90 days) or skip activation |
| Hyper-V not available | Can't test | Document VirtualBox fallback |
| PowerShell Direct fails | Can't automate in-VM | Fall back to network-based WinRM |
| Slow VM performance | Long test times | Use SSD, more RAM |

---

## Decision

**Recommendation**: Start with Phase 1 (Quick Win)

- Get value immediately
- Manual Windows install is ~15 min one-time
- Phase 2 can be added later if needed

Ready to implement? 🚀

---

## Hyper-V Quick-Start (2026-05-03)

### Environment Status
- ✅ **Hyper-V is enabled** on dev box (`Microsoft-Hyper-V-All` feature confirmed)
- 🎯 **Sequencing decision:** smoke-test on dev box first (Stage 3 option A), VM testing comes *after* as the "lurking dev-box residue" catch-net
- 🪄 **Key tool:** `PowerShell Direct` — `Invoke-Command -VMName` and `Copy-VMFile` work over the hypervisor bus. No networking, SSH, or shared folders required. Just admin creds.

### Simplified Scope (overrides Phase 1 task list above)
For 3 stores, we don't need 7 scripts. Build **only two**:

1. `scripts/vm-testing/Reset-AndInstall.ps1` — host-side: restore snapshot → start VM → wait for ready → `Copy-VMFile` installer → `Invoke-Command` to run installer → verify → report
2. `scripts/vm-testing/Test-IndyPOSInstallation.ps1` — runs *inside* the VM via PowerShell Direct, returns structured pass/fail

Skip Phase 2 (autounattend.xml + custom ISO) entirely unless we need to spin up clean VMs frequently.

### Commands Cheat Sheet

#### One-time VM creation
```powershell
# Gen2 VM with Win11-compatible defaults
New-VM -Name "IndyPOS-Test" -Generation 2 -MemoryStartupBytes 4GB `
       -NewVHDPath "C:\VMs\IndyPOS-Test.vhdx" -NewVHDSizeBytes 60GB `
       -SwitchName "Default Switch"

Set-VMProcessor -VMName "IndyPOS-Test" -Count 4
Set-VMMemory   -VMName "IndyPOS-Test" -DynamicMemoryEnabled $true `
               -MinimumBytes 2GB -MaximumBytes 8GB

# Win11 needs TPM
Set-VMKeyProtector -VMName "IndyPOS-Test" -NewLocalKeyProtector
Enable-VMTPM       -VMName "IndyPOS-Test"

# Boot from ISO
Add-VMDvdDrive -VMName "IndyPOS-Test" -Path "C:\ISOs\Win11.iso"
$dvd = Get-VMDvdDrive -VMName "IndyPOS-Test"
Set-VMFirmware -VMName "IndyPOS-Test" -FirstBootDevice $dvd

Start-VM "IndyPOS-Test"
vmconnect.exe localhost "IndyPOS-Test"
# ... install Windows manually (~15 min) ...
```

#### Inside the VM (after Windows install)
```powershell
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
# Optional: speed up tests by disabling Defender RT scan
Set-MpPreference -DisableRealtimeMonitoring $true
```

#### Snapshot the clean state
```powershell
Checkpoint-VM -Name "IndyPOS-Test" -SnapshotName "Clean-Windows"
```

#### The test loop (host-side, what `Reset-AndInstall.ps1` will do)
```powershell
$cred = Get-Credential   # use Windows Credential Manager: "IndyPOS-TestVM"

Restore-VMSnapshot -VMName "IndyPOS-Test" -Name "Clean-Windows" -Confirm:$false
Start-VM "IndyPOS-Test"

# Wait for PowerShell Direct readiness
while (-not (Invoke-Command -VMName "IndyPOS-Test" -Credential $cred `
              -ScriptBlock { $true } -ErrorAction SilentlyContinue)) {
    Start-Sleep -Seconds 5
}

Copy-VMFile -Name "IndyPOS-Test" -SourcePath ".\publish\IndyPOS-Setup.exe" `
            -DestinationPath "C:\Test\IndyPOS-Setup.exe" `
            -CreateFullPath -FileSource Host

Invoke-Command -VMName "IndyPOS-Test" -Credential $cred -ScriptBlock {
    Start-Process "C:\Test\IndyPOS-Setup.exe" -Wait
    # Verification checks (will move into Test-IndyPOSInstallation.ps1)
    Test-Path "C:\ProgramData\IndyPOS\v4.0.0"
    Get-Service postgresql* | Select-Object Name, Status
}
```

### Prerequisites Still Needed
- [ ] Windows 11 ISO downloaded (Microsoft eval — no key required, 90-day validity)
- [ ] Decide VM storage location (default suggestion: `C:\VMs\`)
- [ ] Save VM admin credentials to Windows Credential Manager as `IndyPOS-TestVM`

### When to Pick This Up
After Stage 3 smoke-test on dev box passes. VM run is the final gate before tagging v4.0.0 release.
