# VM Testing Guide (Hyper-V)

This guide explains how to set up a Hyper-V virtual machine for testing IndyPOS installers and deployments.

## Why Use a VM?

- **Clean environment**: Test installer on fresh Windows (no dev tools)
- **Safe rollback**: Snapshots let you restore to known-good state
- **Isolated**: Won't affect your development machine
- **Realistic**: Simulates actual store deployment

---

## Prerequisites

- Windows 10/11 Pro or Enterprise (Hyper-V not available on Home edition)
- At least 8GB RAM (4GB for host + 4GB for VM)
- ~60GB free disk space
- Virtualization enabled in BIOS (usually VT-x or AMD-V)

---

## 1. Enable Hyper-V

Run PowerShell as Administrator:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

**Restart your PC** after this completes.

### Verify Installation

After restart, search for "Hyper-V Manager" in Start menu. If it opens, you're ready!

---

## 2. Download Windows ISO

### Option A: Windows 11 Dev VM (Easiest)

Pre-configured VM with everything set up:

1. Go to: https://developer.microsoft.com/en-us/windows/downloads/virtual-machines/
2. Select **Hyper-V** format
3. Download (~20GB zip file)
4. Extract to `C:\VMs\`

### Option B: Windows 11 ISO (Fresh Install)

For a completely clean install:

1. Go to: https://www.microsoft.com/software-download/windows11
2. Download ISO (~5GB)
3. Save to `C:\ISOs\`

---

## 3. Create Virtual Machine

### Using Pre-built VM (Option A)

1. Open **Hyper-V Manager**
2. Click **Import Virtual Machine** (right panel)
3. Browse to extracted VM folder
4. Select **Copy the virtual machine**
5. Click **Finish**

### Creating from ISO (Option B)

1. Open **Hyper-V Manager**
2. Right-click your PC name → **New** → **Virtual Machine**
3. Configure:

| Setting | Value |
|---------|-------|
| Name | `IndyPOS-Test` |
| Generation | **Generation 2** |
| Memory | `4096` MB (4GB) |
| Network | **Default Switch** |
| Virtual Hard Disk | Create new, 60GB, Dynamic |
| Installation | Browse to Windows ISO |

4. **Before starting**, right-click VM → **Settings**:
   - **Security** → Uncheck "Enable Secure Boot" (if using non-Microsoft ISO)
   - **Processor** → Set to 2-4 virtual processors

5. Start VM and complete Windows installation

---

## 4. Configure VM for Testing

### Enable Enhanced Session Mode (Clipboard/File Sharing)

On your **host machine**, run PowerShell as Admin:

```powershell
Set-VM -VMName "IndyPOS-Test" -EnhancedSessionTransportType HvSocket
```

When connecting to VM, you'll see a dialog - click **Show Options** → enable **Clipboard** and **Drives**.

### Install Guest Additions (if needed)

Inside the VM:
1. Open **Settings** → **Windows Update**
2. Check for updates (installs Hyper-V integration services)

### Disable Windows Defender (Optional)

For faster installer testing:

```powershell
# Inside VM - Run as Admin
Set-MpPreference -DisableRealtimeMonitoring $true
```

---

## 5. Snapshot Workflow

Snapshots are your safety net. Use them liberally!

### Create Snapshot

```
Right-click VM → Checkpoint
```

Or use keyboard: Select VM → `Ctrl+N`

### Recommended Snapshots

| Snapshot Name | When to Create |
|---------------|----------------|
| `01-Clean-Windows` | After Windows setup, before anything installed |
| `02-PostgreSQL-Installed` | After PostgreSQL installation |
| `03-IndyPOS-Installed` | After IndyPOS installer completes |
| `04-First-Run-Complete` | After store configuration wizard |

### Restore Snapshot

```
Right-click checkpoint → Apply
```

### Delete Old Snapshots

Snapshots use disk space. Delete when no longer needed:
```
Right-click checkpoint → Delete Checkpoint
```

---

## 6. Testing IndyPOS Installer

### Prepare Test Files

Copy these to VM (via Enhanced Session drag-drop or shared folder):

```
From Host:                              To VM:
─────────────────────────────────────────────────────────
releases\IndyPOS-Setup.exe      →    C:\Test\
releases\RELEASES               →    C:\Test\
```

### Test Sequence

1. **Apply snapshot**: `01-Clean-Windows`
2. **Copy installer** to VM
3. **Run installer**: Double-click `IndyPOS-Setup.exe`
4. **Observe**:
   - Does PostgreSQL install correctly?
   - Does first-run wizard appear?
   - Any error dialogs?
5. **Create snapshot**: `03-IndyPOS-Installed`
6. **Test functionality**:
   - Login works?
   - Can complete a sale?
   - Printer detection?
7. **Test update** (if applicable):
   - Place new version in update location
   - Restart app
   - Does auto-update work?

### Test Matrix

| Scenario | Steps | Expected Result |
|----------|-------|-----------------|
| Fresh install | Clean Windows → Run installer | App launches, wizard appears |
| Upgrade | Install v1 → Install v2 | Data preserved, new version runs |
| Offline install | Disconnect network → Run installer | Installer completes (bundled deps) |
| Rollback | Install → Uninstall → Reinstall | Clean slate each time |

---

## 7. Common Operations

### Start/Stop VM

| Action | Method |
|--------|--------|
| Start | Right-click VM → **Start** |
| Connect | Right-click VM → **Connect** |
| Shut down | Inside VM: Start → Shut down |
| Force off | Right-click VM → **Turn Off** (use sparingly) |

### Copy Files to VM

**Method 1: Enhanced Session (Recommended)**
1. Connect with Enhanced Session enabled
2. Drag and drop files into VM window

**Method 2: Shared Folder**
```powershell
# On host - share a folder
New-SmbShare -Name "VMShare" -Path "C:\VMShare" -FullAccess "Everyone"
```

Inside VM:
```
\\HOST-PC-NAME\VMShare
```

**Method 3: ISO File**
1. Create ISO with files using any ISO creator tool
2. In Hyper-V Settings → DVD Drive → Browse to ISO

### Check VM IP Address

Inside VM:
```powershell
ipconfig
```

Or from host:
```powershell
Get-VM -Name "IndyPOS-Test" | Get-VMNetworkAdapter | Select IPAddresses
```

---

## 8. Troubleshooting

### VM Won't Start

**Error: "Hyper-V cannot be started because the hypervisor is not running"**

```powershell
# Run as Admin on host
bcdedit /set hypervisorlaunchtype auto
# Restart host PC
```

**Error: "The virtual machine could not be started because the hypervisor is not running"**

1. Check BIOS: Enable VT-x / AMD-V
2. Disable other hypervisors (VirtualBox, VMware)

### No Network in VM

1. Check **Default Switch** exists in Hyper-V Manager → Virtual Switch Manager
2. If missing, create new **Internal** switch
3. Assign switch to VM in Settings → Network Adapter

### Slow Performance

- Increase RAM: Settings → Memory → 8192 MB
- Increase CPUs: Settings → Processor → 4
- Use SSD for VM storage
- Disable Windows Defender in VM

### Enhanced Session Not Working

```powershell
# On host
Set-VMHost -EnableEnhancedSessionMode $true
Set-VM -VMName "IndyPOS-Test" -EnhancedSessionTransportType HvSocket
```

Inside VM: Install all Windows Updates

---

## 9. Cleanup

### Delete VM Completely

1. **Stop VM** if running
2. Right-click VM → **Delete**
3. Manually delete VHD files from `C:\VMs\` if needed

### Reclaim Disk Space

Compact dynamic VHD:
```powershell
Optimize-VHD -Path "C:\VMs\IndyPOS-Test\Virtual Hard Disks\IndyPOS-Test.vhdx" -Mode Full
```

---

## Quick Reference

| Task | Command/Action |
|------|----------------|
| Open Hyper-V | `Win+S` → "Hyper-V Manager" |
| Create snapshot | `Ctrl+N` (with VM selected) |
| Apply snapshot | Right-click checkpoint → Apply |
| Start VM | Right-click → Start |
| Connect to VM | Right-click → Connect |
| Enhanced Session | Settings dialog on connect |

---

## See Also

- [Store Installation Guide](../operations/store-installation-guide.md)
- [Pilot Checklist](../operations/pilot-checklist.md)
- [Troubleshooting Guide](../operations/troubleshooting-guide.md)
