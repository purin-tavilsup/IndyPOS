# IndyPOS Version Management

This document describes how versioning works across IndyPOS components.

## Overview

IndyPOS uses [Semantic Versioning](https://semver.org/) (SemVer):

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]

Examples:
  1.0.0           - Stable release
  1.1.0           - Minor release (new features, backward compatible)
  2.0.0           - Major release (breaking changes)
  1.0.0-beta.1    - Pre-release
  1.0.0+abc123    - With build metadata
```

## Centralized Version Management

All version info is defined in `Directory.Build.props` at the repo root:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <InformationalVersion>1.0.0</InformationalVersion>
</PropertyGroup>
```

This automatically applies to all projects:
- IndyPOS.StoreHub
- IndyPOS.Windows.Forms
- IndyPOS.MigrationTool
- All other projects

## Version Properties

| Property | Format | Purpose |
|----------|--------|---------|
| `Version` | `1.0.0` | NuGet package version, general version |
| `AssemblyVersion` | `1.0.0.0` | .NET runtime assembly binding |
| `FileVersion` | `1.0.0.0` | Windows file properties |
| `InformationalVersion` | `1.0.0-beta.1` | Display version, supports pre-release tags |

## Updating the Version

### Manual Update

Edit `Directory.Build.props`:

```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
<InformationalVersion>1.1.0</InformationalVersion>
```

### CI/CD Override

Pass version at build time:

```bash
dotnet build /p:Version=1.2.0 /p:InformationalVersion=1.2.0-beta.1+abc123
```

## Accessing Version at Runtime

### Using AppVersion Helper

```csharp
using IndyPOS.Application.Common;

var versionInfo = AppVersion.GetVersionInfo();

Console.WriteLine(versionInfo.DisplayVersion);      // "1.0.0"
Console.WriteLine(versionInfo.AssemblyVersion);     // "1.0.0.0"
Console.WriteLine(versionInfo.InformationalVersion); // "1.0.0"

// Check if update available
if (versionInfo.IsOlderThan("1.1.0"))
{
    Console.WriteLine("Update available!");
}
```

### StoreHub Version Endpoint

```http
GET /version
```

Response:
```json
{
  "version": "1.0.0",
  "assemblyVersion": "1.0.0.0",
  "fullVersion": "1.0.0",
  "name": "IndyPOS.StoreHub",
  "environment": "Production"
}
```

No authentication required. Use for:
- Health monitoring
- Troubleshooting
- Update checks

### WinForms Version Display

Version is displayed in the main form footer:
```
Version: 1.0.0
```

## Version Comparison

The `AppVersion.IsOlderThan()` method compares versions:

```csharp
var current = AppVersion.GetVersionInfo();

// Comparing semver versions
current.IsOlderThan("1.0.0");       // false (same)
current.IsOlderThan("1.1.0");       // true (newer available)
current.IsOlderThan("0.9.0");       // false (older)
current.IsOlderThan("2.0.0-beta");  // true (newer available)
```

## Release Workflow

### 1. Update Version

```bash
# Edit Directory.Build.props
<Version>1.1.0</Version>
```

### 2. Build and Test

```bash
dotnet build
dotnet test
```

### 3. Publish

```bash
cd scripts
.\publish.ps1
```

### 4. Tag Release

```bash
git tag -a v1.1.0 -m "Release 1.1.0"
git push origin v1.1.0
```

## Pre-release Versions

For beta/preview releases:

```xml
<Version>1.1.0-beta.1</Version>
<InformationalVersion>1.1.0-beta.1</InformationalVersion>
```

Pre-release ordering:
```
1.0.0-alpha.1 < 1.0.0-beta.1 < 1.0.0-rc.1 < 1.0.0
```

## Future: Auto-Update (Velopack)

This versioning infrastructure is designed to support future Velopack integration:

1. **Update Server** returns latest version info
2. **StoreHub/WinForms** calls `/version` or checks locally
3. **Compare** using `IsOlderThan()`
4. **Download** and apply update if newer

See `.planning/indypos-overhaul/PLAN.md` section "Future: Auto-Update System" for details.

## Files Reference

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Centralized version definition |
| `src/IndyPOS.Application/Common/AppVersion.cs` | Version helper class |
| `src/IndyPOS.StoreHub/Program.cs` | `/version` endpoint |
| `src/IndyPOS.Windows.Forms/Machine.cs` | WinForms version display |

## Troubleshooting

### Version shows 0.0.0

Check that `Directory.Build.props` exists at repo root and contains valid `<Version>` element.

### Pre-release tag not showing

Ensure `InformationalVersion` is set in `Directory.Build.props`.

### Different versions in different components

All components should use the same version from `Directory.Build.props`. If they differ, ensure:
- No local `<Version>` override in individual `.csproj` files
- Build was done from repo root (not individual project)
