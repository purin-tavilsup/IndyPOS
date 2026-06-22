# IndyPOS.Vault — DPAPI Secret Protection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Encrypt the StoreHub DB connection string and JWT signing key at rest in `appsettings.json` using Windows DPAPI, so the file is useless off the machine that created it.

**Architecture:** A new zero-dependency leaf project `IndyPOS.Vault` exposes a stateless `SecretProtector` (DPAPI machine scope, `DPAPI:`-marker, per-key entropy). The standalone installer encrypts the two secrets when writing `appsettings.json`; the StoreHub service decrypts them at startup via a `ConfigurationManager` extension before any consumer reads them. Dev/Aspire is unaffected (unmarked values → no-op).

**Tech Stack:** C# .NET 10, `System.Security.Cryptography.ProtectedData`, ASP.NET Core configuration, xUnit + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-06-21-vault-dpapi-secret-protection-design.md`

## Global Constraints

- **Target framework:** `net10.0-windows` for Vault and its test project (DPAPI is Windows-only).
- **Vault is a strict zero-dependency leaf:** it may reference only the BCL + `System.Security.Cryptography.ProtectedData`. It must NOT reference Application/Infrastructure/Domain/ServiceDefaults. An architecture test enforces this.
- **DPAPI scope:** `DataProtectionScope.LocalMachine` (installer encrypts as admin; service decrypts as LocalSystem).
- **Entropy:** per-key, `SHA256.HashData(Encoding.UTF8.GetBytes($"IndyPOS:{key}"))`.
- **Marker:** `DPAPI:` — `IsProtected` is an ordinal, case-sensitive `StartsWith`.
- **Encoding:** always `Encoding.UTF8` for plaintext ↔ bytes.
- **Error handling:** decryption failure FAILS FAST (throws), names the offending key, never logs/leaks the secret, never falls back to ciphertext.
- **Test framework:** xUnit (`[Fact]`/`[Theory]`+`[InlineData]`), `Method_Condition_ShouldExpectedBehavior` naming, `[SupportedOSPlatform("windows")]` on DPAPI tests.
- **Protected config keys (exact):** `ConnectionStrings:storehub-db` and `LocalToken:SecretKey`.

---

### Task 1: IndyPOS.Vault project + SecretProtector

**Files:**
- Create: `src/IndyPOS.Vault/IndyPOS.Vault.csproj`
- Create: `src/IndyPOS.Vault/SecretProtector.cs`
- Create: `tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj`
- Create: `tests/IndyPOS.Vault.Tests/SecretProtectorTests.cs`
- Create: `tests/IndyPOS.Vault.Tests/VaultArchitectureTests.cs`
- Modify: `IndyPOS.sln` (via `dotnet sln add`)

**Interfaces:**
- Consumes: nothing (leaf).
- Produces:
  - `IndyPOS.Vault.SecretProtector.Protect(string key, string plaintext) -> string` (returns `"DPAPI:<base64>"`; throws `InvalidOperationException` if `plaintext` already protected; `ArgumentException` on null/empty key; `ArgumentNullException` on null plaintext)
  - `IndyPOS.Vault.SecretProtector.Unprotect(string key, string value) -> string` (unmarked → returns input; marked → decrypted plaintext; throws `CryptographicException`/`FormatException` on failure)
  - `IndyPOS.Vault.SecretProtector.IsProtected(string value) -> bool`

- [ ] **Step 1: Create the Vault project file**

Create `src/IndyPOS.Vault/IndyPOS.Vault.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="IndyPOS.Vault.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the Vault.Tests project file**

Create `tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj` (mirrors `IndyPOS.Bootstrapper.Tests.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.Vault\IndyPOS.Vault.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add both projects to the solution**

Run:
```bash
dotnet sln IndyPOS.sln add src/IndyPOS.Vault/IndyPOS.Vault.csproj --solution-folder Core
dotnet sln IndyPOS.sln add tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj --solution-folder Tests
```
Expected: "Project ... added to the solution." for both.

- [ ] **Step 4: Write the failing tests**

Create `tests/IndyPOS.Vault.Tests/SecretProtectorTests.cs`:

```csharp
using System.Runtime.Versioning;
using System.Security.Cryptography;
using FluentAssertions;
using IndyPOS.Vault;
using Xunit;

namespace IndyPOS.Vault.Tests;

[SupportedOSPlatform("windows")]
public class SecretProtectorTests
{
    private const string Key = "ConnectionStrings:storehub-db";

    [Theory]
    [InlineData("Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=Ab12Cd34")]
    [InlineData("dGhpcyBpcyBhIDY0IGJ5dGUga2V5IGV4YW1wbGUgZm9yIHRlc3RpbmcgcHVycG9zZXM=")]
    [InlineData("ünîçödé-secret-Ω")]
    [InlineData("")]
    public void Protect_ThenUnprotect_ShouldRoundTripVariousPayloads(string plaintext)
    {
        var protectedValue = SecretProtector.Protect(Key, plaintext);

        SecretProtector.Unprotect(Key, protectedValue).Should().Be(plaintext);
    }

    [Fact]
    public void Protect_ShouldProduceMarkerPrefixedValue()
    {
        var result = SecretProtector.Protect(Key, "secret");

        result.Should().StartWith("DPAPI:");
    }

    [Fact]
    public void Unprotect_WithUnmarkedValue_ShouldReturnInputUnchanged()
    {
        const string plain = "Host=127.0.0.1;Password=plain";

        SecretProtector.Unprotect(Key, plain).Should().Be(plain);
    }

    [Theory]
    [InlineData("DPAPI:abc", true)]
    [InlineData("dpapi:abc", false)]
    [InlineData(" DPAPI:abc", false)]
    [InlineData("plain DPAPI: value", false)]
    [InlineData("", false)]
    public void IsProtected_WithVariousValues_ShouldReturnExpected(string value, bool expected)
    {
        SecretProtector.IsProtected(value).Should().Be(expected);
    }

    [Fact]
    public void Protect_OnAlreadyProtectedValue_ShouldThrow()
    {
        var once = SecretProtector.Protect(Key, "secret");

        var act = () => SecretProtector.Protect(Key, once);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Protect_WithNullPlaintext_ShouldThrowArgumentNullException()
    {
        var act = () => SecretProtector.Protect(Key, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unprotect_WithMarkerAndNonBase64Payload_ShouldThrowFormatException()
    {
        var act = () => SecretProtector.Unprotect(Key, "DPAPI:!!!not-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Unprotect_WithValidBase64ButNonDpapiBytes_ShouldThrowCryptographicException()
    {
        var bogus = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        var act = () => SecretProtector.Unprotect(Key, bogus);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_WithWrongEntropyKey_ShouldThrow()
    {
        var protectedValue = SecretProtector.Protect("LocalToken:SecretKey", "secret");

        var act = () => SecretProtector.Unprotect("ConnectionStrings:storehub-db", protectedValue);

        act.Should().Throw<CryptographicException>();
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj`
Expected: FAIL — `SecretProtector` does not exist (compile error).

- [ ] **Step 6: Implement SecretProtector**

Create `src/IndyPOS.Vault/SecretProtector.cs`:

```csharp
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace IndyPOS.Vault;

/// <summary>
/// Stateless DPAPI seal/unseal for at-rest secrets in config files.
/// Machine-scoped: the installer (admin) encrypts; the service (LocalSystem)
/// decrypts on the same machine. Values are marked <c>DPAPI:&lt;base64&gt;</c> and
/// bound to their config key via per-key entropy.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SecretProtector
{
    private const string Marker = "DPAPI:";

    public static bool IsProtected(string value) =>
        value is not null && value.StartsWith(Marker, StringComparison.Ordinal);

    public static string Protect(string key, string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(plaintext);

        if (IsProtected(plaintext))
        {
            throw new InvalidOperationException(
                "Value is already protected; refusing to double-wrap.");
        }

        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext),
            GetEntropy(key),
            DataProtectionScope.LocalMachine);

        return Marker + Convert.ToBase64String(encrypted);
    }

    public static string Unprotect(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        if (!IsProtected(value))
        {
            return value;
        }

        var encrypted = Convert.FromBase64String(value[Marker.Length..]);

        var plainBytes = ProtectedData.Unprotect(
            encrypted,
            GetEntropy(key),
            DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // Binds each ciphertext to its config key, so a value protected under one
    // key cannot be decrypted under another (prevents field substitution).
    private static byte[] GetEntropy(string key) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"IndyPOS:{key}"));
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj`
Expected: PASS (all SecretProtector tests green).

- [ ] **Step 8: Write the architecture guard test**

Create `tests/IndyPOS.Vault.Tests/VaultArchitectureTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Vault;
using Xunit;

namespace IndyPOS.Vault.Tests;

public class VaultArchitectureTests
{
    [Fact]
    public void Vault_ShouldNotReferenceApplicationInfrastructureOrDomain()
    {
        var referenced = typeof(SecretProtector).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        referenced.Should().NotContain(name =>
            name == "IndyPOS.Application" ||
            name == "IndyPOS.Infrastructure" ||
            name == "IndyPOS.Domain" ||
            name == "IndyPOS.ServiceDefaults");
    }
}
```

- [ ] **Step 9: Run the architecture test**

Run: `dotnet test tests/IndyPOS.Vault.Tests/IndyPOS.Vault.Tests.csproj`
Expected: PASS.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Vault tests/IndyPOS.Vault.Tests IndyPOS.sln
git commit -m "feat(vault): add IndyPOS.Vault SecretProtector with DPAPI machine-scope sealing"
```

---

### Task 2: StoreHub startup decryption

**Files:**
- Create: `src/IndyPOS.StoreHub/Configuration/SecretConfigurationExtensions.cs`
- Modify: `src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj` (add Vault ProjectReference)
- Modify: `src/IndyPOS.StoreHub/Program.cs:37` (call `UnprotectSecrets` after `CreateBuilder`)
- Create: `tests/IndyPOS.StoreHub.IntegrationTests/Configuration/SecretConfigurationExtensionsTests.cs`

**Interfaces:**
- Consumes: `IndyPOS.Vault.SecretProtector.{Protect,Unprotect,IsProtected}`.
- Produces: `IndyPOS.StoreHub.Configuration.SecretConfigurationExtensions.UnprotectSecrets(this ConfigurationManager configuration, params string[] keys) -> void` (decrypts marked keys in place; throws `InvalidOperationException` naming the key on decrypt failure).

- [ ] **Step 1: Add the Vault project reference to StoreHub**

In `src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj`, add to the existing `ItemGroup` of `ProjectReference`s (after the Infrastructure reference at line 26):

```xml
    <ProjectReference Include="..\IndyPOS.Vault\IndyPOS.Vault.csproj" />
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IndyPOS.StoreHub.IntegrationTests/Configuration/SecretConfigurationExtensionsTests.cs`:

```csharp
using System.Runtime.Versioning;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.StoreHub.Configuration;
using IndyPOS.Vault;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Configuration;

[SupportedOSPlatform("windows")]
public class SecretConfigurationExtensionsTests
{
    private const string ConnKey = "ConnectionStrings:storehub-db";
    private const string JwtKey = "LocalToken:SecretKey";

    private static ConfigurationManager BuildConfig(Dictionary<string, string?> values)
    {
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(values);
        return config;
    }

    [Fact]
    public void UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaGetConnectionString()
    {
        const string conn = "Host=127.0.0.1;Port=5432;Database=db;Username=u;Password=p";
        var config = BuildConfig(new() { [ConnKey] = SecretProtector.Protect(ConnKey, conn) });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config.GetConnectionString("storehub-db").Should().Be(conn);
    }

    [Fact]
    public void UnprotectSecrets_WithProtectedValue_ShouldExposePlaintextViaLocalTokenOptionsBinding()
    {
        const string secret = "a-64-byte-base64-signing-key-value-for-testing-purposes==";
        var config = BuildConfig(new() { [JwtKey] = SecretProtector.Protect(JwtKey, secret) });

        config.UnprotectSecrets(ConnKey, JwtKey);

        var options = config.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>();
        options!.SecretKey.Should().Be(secret);
    }

    [Fact]
    public void UnprotectSecrets_WithUnmarkedValue_ShouldLeaveConfigurationUnchanged()
    {
        const string conn = "Host=127.0.0.1;Password=plain";
        var config = BuildConfig(new() { [ConnKey] = conn });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config[ConnKey].Should().Be(conn);
    }

    [Fact]
    public void UnprotectSecrets_WithMissingKey_ShouldNoOp()
    {
        var config = BuildConfig(new() { ["Unrelated"] = "x" });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().NotThrow();
    }

    [Fact]
    public void UnprotectSecrets_WithOneKeyProtectedAndOnePlaintext_ShouldDecryptOnlyProtected()
    {
        const string conn = "Host=127.0.0.1;Password=p";
        const string plainJwt = "plain-jwt";
        var config = BuildConfig(new()
        {
            [ConnKey] = SecretProtector.Protect(ConnKey, conn),
            [JwtKey] = plainJwt
        });

        config.UnprotectSecrets(ConnKey, JwtKey);

        config.GetConnectionString("storehub-db").Should().Be(conn);
        config[JwtKey].Should().Be(plainJwt);
    }

    [Fact]
    public void UnprotectSecrets_WhenDecryptFails_ShouldThrowNamingTheKey()
    {
        var corrupt = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var config = BuildConfig(new() { [ConnKey] = corrupt });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*{ConnKey}*");
    }

    [Fact]
    public void UnprotectSecrets_WhenDecryptFails_ShouldNotExposeRawCiphertext()
    {
        var corrupt = "DPAPI:" + Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var config = BuildConfig(new() { [ConnKey] = corrupt });

        var act = () => config.UnprotectSecrets(ConnKey, JwtKey);

        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().NotContain(corrupt);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SecretConfigurationExtensionsTests"`
Expected: FAIL — `SecretConfigurationExtensions` / `UnprotectSecrets` not found (compile error).

- [ ] **Step 4: Implement the extension**

Create `src/IndyPOS.StoreHub/Configuration/SecretConfigurationExtensions.cs`:

```csharp
using System.Runtime.Versioning;
using System.Security.Cryptography;
using IndyPOS.Vault;
using Microsoft.Extensions.Configuration;

namespace IndyPOS.StoreHub.Configuration;

/// <summary>
/// Decrypts DPAPI-protected configuration values in place at startup, before any
/// consumer (DbContext, JWT) reads them. Unmarked or absent keys are left as-is,
/// so dev/Aspire (which inject an unmarked connection string) are a no-op.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SecretConfigurationExtensions
{
    public static void UnprotectSecrets(this ConfigurationManager configuration, params string[] keys)
    {
        var overrides = new Dictionary<string, string?>();

        foreach (var key in keys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value) || !SecretProtector.IsProtected(value))
            {
                continue;
            }

            overrides[key] = Decrypt(key, value);
        }

        if (overrides.Count > 0)
        {
            // Appended last → wins over the JSON file source for these keys.
            configuration.AddInMemoryCollection(overrides);
        }
    }

    private static string Decrypt(string key, string value)
    {
        try
        {
            return SecretProtector.Unprotect(key, value);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new InvalidOperationException(
                $"Could not decrypt configuration key '{key}'. This config was likely created " +
                "on a different machine, or the value is corrupt. The service cannot start.", ex);
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SecretConfigurationExtensionsTests"`
Expected: PASS.

- [ ] **Step 6: Wire the call into Program.cs**

In `src/IndyPOS.StoreHub/Program.cs`, add the using near the other `using IndyPOS.*` lines (e.g. after line 30):

```csharp
using IndyPOS.StoreHub.Configuration;
```

Then immediately after line 37 (`var builder = WebApplication.CreateBuilder(args);`), insert:

```csharp

// Decrypt DPAPI-protected secrets before any consumer reads them. No-op in dev:
// Aspire injects an unmarked connection string. See IndyPOS.Vault.
builder.Configuration.UnprotectSecrets(
    "ConnectionStrings:storehub-db",
    "LocalToken:SecretKey");
```

- [ ] **Step 7: Build StoreHub to verify it compiles**

Run: `dotnet build src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.StoreHub tests/IndyPOS.StoreHub.IntegrationTests
git commit -m "feat(storehub): decrypt DPAPI-protected secrets at startup via UnprotectSecrets"
```

---

### Task 3: Installer encrypts secrets + drops storehub.key

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj` (add Vault ProjectReference + InternalsVisibleTo)
- Modify: `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs` (encrypt secrets; drop key file; ACL appsettings.json; extract testable JSON builder)
- Modify: `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs` (add config-protection tests)

**Interfaces:**
- Consumes: `IndyPOS.Vault.SecretProtector.{Protect,IsProtected}`.
- Produces (internal, for tests): `DatabaseSetup.BuildStoreHubConfigJson(InstallationConfig config, string jwtSecret) -> string`; `DatabaseSetup.GenerateJwtSecret() -> string`.

- [ ] **Step 1: Add the Vault reference + InternalsVisibleTo to the installer**

In `installer/IndyPOS.Bootstrapper/IndyPOS.Bootstrapper.csproj`, add a new `ItemGroup`:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\IndyPOS.Vault\IndyPOS.Vault.csproj" />
    <InternalsVisibleTo Include="IndyPOS.Bootstrapper.Tests" />
  </ItemGroup>
```

- [ ] **Step 2: Write the failing tests**

In `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs`, add these tests (add `using IndyPOS.Vault;` and `using System.Text.Json;` if missing):

```csharp
[Fact]
public void BuildStoreHubConfigJson_ShouldProtectConnectionStringAndSecretKey()
{
    var config = new InstallationConfig { StoreId = "STORE-001", AdminPassword = "pw" };

    var json = DatabaseSetup.BuildStoreHubConfigJson(config, "jwt-secret-value");

    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    root.GetProperty("connectionStrings").GetProperty("storehub-db")
        .GetString().Should().StartWith("DPAPI:");
    root.GetProperty("localToken").GetProperty("secretKey")
        .GetString().Should().StartWith("DPAPI:");
}

[Fact]
public void BuildStoreHubConfigJson_ShouldLeaveNonSecretFieldsPlaintext()
{
    var config = new InstallationConfig { StoreId = "STORE-001", AdminPassword = "pw" };

    var json = DatabaseSetup.BuildStoreHubConfigJson(config, "jwt-secret-value");

    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    root.GetProperty("storeIdentity").GetProperty("storeId").GetString().Should().Be("STORE-001");
    root.GetProperty("localToken").GetProperty("issuer").GetString().Should().Be("IndyPOS.StoreHub");
}

[Fact]
public void GenerateJwtSecret_ShouldReturnBase64Of64Bytes()
{
    var secret = DatabaseSetup.GenerateJwtSecret();

    Convert.FromBase64String(secret).Length.Should().Be(64);
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter "FullyQualifiedName~DatabaseSetupTests"`
Expected: FAIL — `BuildStoreHubConfigJson` / `GenerateJwtSecret` not found (compile error).

- [ ] **Step 4: Refactor `GenerateJwtSecret` to drop the key file**

In `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`, replace the `GenerateJwtSecretAsync` method (lines ~134-153) AND the now-unused `RestrictFilePermissions` is RETAINED but repurposed (see Step 6). Replace `GenerateJwtSecretAsync`:

```csharp
    // 64-byte (512-bit) random signing key. No longer persisted to a separate
    // file — it is DPAPI-protected inside appsettings.json (the only consumer).
    // A fresh secret is generated per install; 12h token expiry makes that fine.
    internal static string GenerateJwtSecret()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
```

Update the call site at line ~56 from:
```csharp
            log?.Report("Generating JWT secret key...");
            var jwtSecret = await GenerateJwtSecretAsync(cancellationToken);
```
to:
```csharp
            log?.Report("Generating JWT secret key...");
            var jwtSecret = GenerateJwtSecret();
```

- [ ] **Step 5: Extract `BuildStoreHubConfigJson` and protect the two secrets**

In `DatabaseSetup.cs`, change `CreateStoreHubConfigAsync` to delegate JSON construction to a new testable method. Add `using IndyPOS.Vault;` at the top. Replace the body of `CreateStoreHubConfigAsync` (lines ~311-381) so the connection string + JWT are protected, the JSON build is extracted, and the file is ACL-locked after writing:

```csharp
    private async Task CreateStoreHubConfigAsync(string jwtSecret, CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(Config.StoreHubInstallPath, "appsettings.json");

        var json = BuildStoreHubConfigJson(Config, jwtSecret);

        await File.WriteAllTextAsync(configPath, json, cancellationToken);

        // appsettings.json now holds the protected secrets; lock it down to
        // Administrators + LocalSystem as defense-in-depth alongside DPAPI.
        RestrictFilePermissions(configPath);
    }

    internal static string BuildStoreHubConfigJson(InstallationConfig config, string jwtSecret)
    {
        var connectionString =
            $"Host=127.0.0.1;Port=5432;Database={config.DatabaseName};Username={config.AppUser};Password={config.AppPassword}";

        var configObject = new
        {
            Urls = $"http://localhost:{config.HealthCheckPort}",
            AllowedHosts = "*",
            ConnectionStrings = new
            {
                storehub_db = SecretProtector.Protect("ConnectionStrings:storehub-db", connectionString)
            },
            LocalToken = new
            {
                SecretKey = SecretProtector.Protect("LocalToken:SecretKey", jwtSecret),
                Issuer = "IndyPOS.StoreHub",
                Audience = "IndyPOS.POS",
                ExpiryHours = 12
            },
            StoreIdentity = new
            {
                StoreId = config.StoreId
            },
            InitialAdmin = new
            {
                Username = config.AdminUsername,
                Password = config.AdminPassword
            },
            CloudApi = new
            {
                BaseUrl = "",
                ClientId = "",
                ClientSecret = "",
                Scopes = "sync.write master.read"
            },
            SyncWorker = new
            {
                Enabled = false
            },
            Logging = new
            {
                LogLevel = new
                {
                    Default = "Warning",
                    Microsoft_AspNetCore = "Warning",
                    Microsoft_EntityFrameworkCore = "Warning"
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(configObject, options);

        json = json.Replace("\"storehub_db\"", "\"storehub-db\"");
        json = json.Replace("\"Microsoft_AspNetCore\"", "\"Microsoft.AspNetCore\"");
        json = json.Replace("\"Microsoft_EntityFrameworkCore\"", "\"Microsoft.EntityFrameworkCore\"");

        return json;
    }
```

- [ ] **Step 6: Run the new unit tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter "FullyQualifiedName~DatabaseSetupTests"`
Expected: PASS (including the three new tests).

- [ ] **Step 7: Run the full bootstrapper test suite (regression)**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: PASS (previously-passing tests still green; skips unchanged).

- [ ] **Step 8: Commit**

```bash
git add installer/IndyPOS.Bootstrapper tests/IndyPOS.Bootstrapper.Tests
git commit -m "feat(installer): DPAPI-protect storehub secrets, drop plaintext storehub.key, ACL appsettings.json"
```

---

### Task 4: Update scripts + docs for the dropped key file

**Files:**
- Modify: `scripts/verify-install.ps1:255-267` (remove `storehub.key` check)
- Modify: `scripts/install-config.ps1:40-42, 218-225` (remove `storehub.key` references)
- Modify: `docs/operations/store-installation-guide.md` (remove `storehub.key` mentions)

**Interfaces:**
- Consumes: nothing (tooling/docs only).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Read the affected regions**

Run:
```bash
grep -n "storehub.key" scripts/verify-install.ps1 scripts/install-config.ps1 docs/operations/store-installation-guide.md
```
Expected: line numbers for each reference (verify-install.ps1 ~257-266; install-config.ps1 ~41, ~220).

- [ ] **Step 2: Remove the `storehub.key` check from verify-install.ps1**

In `scripts/verify-install.ps1`, delete the block that probes `storehub.key` (the `$jwtKey = Join-Path $KeysDirectory 'storehub.key'` check and its Pass/Fail branches, ~lines 257-266). Replace it with a comment-free removal — the JWT key now lives DPAPI-protected inside `appsettings.json` and is validated by the service-start + login checks. If a JWT-presence check is still desired, assert that `appsettings.json` contains a `localToken.secretKey` starting with `DPAPI:` instead.

- [ ] **Step 3: Remove the `storehub.key` references from install-config.ps1**

In `scripts/install-config.ps1`, remove the documentation line referencing `C:\ProgramData\IndyPOS\keys\storehub.key` (~line 41) and the `$keyPath = "...storehub.key"` logic (~line 220) and any block that reads/writes it.

- [ ] **Step 4: Update the operations guide**

In `docs/operations/store-installation-guide.md`, remove or update any sentence describing the `storehub.key` file so it reflects that the JWT key is stored DPAPI-protected in `appsettings.json`.

- [ ] **Step 5: Verify no stray references remain**

Run:
```bash
grep -rn "storehub.key" scripts/ docs/ installer/ src/
```
Expected: no matches (or only historical `.planning/` / spec references, which are fine).

- [ ] **Step 6: Commit**

```bash
git add scripts/verify-install.ps1 scripts/install-config.ps1 docs/operations/store-installation-guide.md
git commit -m "chore: drop storehub.key references from scripts and docs"
```

---

### Task 5: Full build, test, and VM end-to-end validation

**Files:** none (validation only).

- [ ] **Step 1: Full solution build**

Run: `dotnet build IndyPOS.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Full test run**

Run: `dotnet test IndyPOS.sln`
Expected: All tests pass (Vault, StoreHub config extension, bootstrapper, Application). DPAPI tests run on Windows.

- [ ] **Step 3: Rebuild the installer**

Follow the existing installer build process to produce `publish\IndyPOS-Setup.exe` at v4.0.0 with the new Vault assembly bundled.

- [ ] **Step 4: VM validation — happy path**

On the clean Hyper-V `IndyPOS-Test` VM (`Clean-Windows` snapshot), run `.\scripts\vm-testing\Reset-AndInstall.ps1 -KeepRunning` and confirm:
  1. `C:\ProgramData\IndyPOS\v4\StoreHub\appsettings.json` shows `DPAPI:` on both `connectionStrings.storehub-db` and `localToken.secretKey`; all other fields plaintext.
  2. No `C:\ProgramData\IndyPOS\v4\keys\storehub.key` file exists.
  3. `appsettings.json` ACL is restricted to Administrators + LocalSystem.
  4. StoreHub service reaches healthy (`/health/ready`).
  5. A real POS login succeeds (validates both the decrypted DB password and the decrypted JWT signing key end-to-end).

- [ ] **Step 5: VM validation — negative (the security claim)**

Copy `appsettings.json` to a second VM (or a reverted-to-pre-install snapshot), start StoreHub there, and confirm it FAILS FAST with a clear, key-naming error and does NOT start. Record where the error surfaces (app log vs Windows Event Log).

- [ ] **Step 6: VM validation — re-run idempotency**

Re-run the installer on the same box; confirm fresh secrets are generated, both still `DPAPI:`-marked, and the service restarts cleanly.

- [ ] **Step 7: Final commit / status update**

Update `.claude/STATUS.md` and `.claude/session-log.md` with the completed Vault work and VM-validation result.

---

## Self-Review Notes

- **Spec coverage:** Vault leaf + contract (Task 1); StoreHub decrypt + ordering/binding tests + fail-fast (Task 2); installer encrypt + drop key file + ACL parity (Task 3); scripts/docs touch-points (Task 4); xUnit framework + VM checklist (Tasks 1-5). All spec sections mapped.
- **Wrong-machine test:** intentionally validated via VM Step 5 (not a portable unit test); the crypto-failure code path is covered by `Unprotect_WithValidBase64ButNonDpapiBytes_ShouldThrowCryptographicException` and `Unprotect_WithWrongEntropyKey_ShouldThrow`.
- **Type consistency:** `Protect(key, plaintext)` / `Unprotect(key, value)` / `IsProtected(value)` used identically across Tasks 1-3; `UnprotectSecrets(params string[])` and `BuildStoreHubConfigJson`/`GenerateJwtSecret` signatures match their call sites.
