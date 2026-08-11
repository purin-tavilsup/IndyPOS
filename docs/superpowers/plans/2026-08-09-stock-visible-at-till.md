# Stock Visible At The Till — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make migrated stock visible in `InventoryPanel` and correctable through the product edit form, so defect 14's fix can be confirmed at a real till.

**Architecture:** Stock stays defined as `SUM(InventoryMovement.QuantityDelta)` — no snapshot column. A new batched repository method feeds a new `GET /products/stock` endpoint; the WinForms inventory service merges those balances into `InventoryProductDto` on its list paths only, so the product cache never holds a stale quantity. Separately, `AdjustQuantityRequest` switches from target-quantity to delta semantics and the edit form finally calls it.

**Tech Stack:** C# .NET 10, ASP.NET Core Minimal APIs, EF Core + PostgreSQL, Windows.Forms, xUnit + FluentAssertions + Moq, Testcontainers-backed integration tests.

**Spec:** `docs/superpowers/specs/2026-08-09-stock-visible-at-till-design.md`

## Global Constraints

- Migrations are **forward-only and additive**. This plan adds **no** EF migration and **no** schema change — if a task seems to need one, stop and re-read the spec.
- Store scope always comes from `IStoreIdentityService`, never from a caller-supplied value.
- Test naming: `Method_Condition_ShouldExpectedBehavior`. Arrange-Act-Assert separated by blank lines.
- Fluent calls vertically aligned on the dot.
- File-scoped namespaces; `_camelCase` private fields; records for DTOs.
- Docker must be running for `IndyPOS.StoreHub.IntegrationTests`.
- Baseline before starting: `dotnet test IndyPOS.sln` → **551 pass / 1 skip / 0 fail**.
- Branch: `feat/stock-visible-at-till` (already created, spec committed at `07be05d`).

## File Structure

**Create:**
- `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/ProductStockDto.cs` — `(Guid ProductId, int Quantity)` wire record
- `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQuery.cs` — query, optional single-product filter
- `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQueryHandler.cs` — reads balances for the current store
- `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustQuantityResponse.cs` — `(Guid ProductId, int Quantity)`, replaces the wrong `ProductDto` return
- `src/IndyPOS.Windows.Forms/UI/Inventory/PendingStockAdjustment.cs` — pure accumulator for the +/- buttons
- `tests/IndyPOS.StoreHub.IntegrationTests/InventoryMovementRepositoryTests.cs`
- `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductStockEndpointTests.cs`
- `tests/IndyPOS.Windows.Forms.Tests/UI/Inventory/PendingStockAdjustmentTests.cs`

**Modify:**
- `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IInventoryMovementRepository.cs` — add `GetBalancesAsync`
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/InventoryMovementRepository.cs` — implement it
- `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustQuantityRequest.cs` — `TargetQuantity` → `Delta`
- `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommand.cs` — same
- `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommandHandler.cs` — write the delta directly
- `src/IndyPOS.StoreHub/Program.cs` — register the query handler, add `GET /products/stock`, update the adjust endpoint
- `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` — add `GetProductStockAsync`, fix adjust return type
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` — implement both
- `src/IndyPOS.Application/Common/Interfaces/IInventoryProductService.cs` — `targetQuantity` → `delta`; drop `QuantityInStock` from `UpdateInventoryProductRequest`
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubInventoryProductService.cs` — merge balances, delete the TODO
- `src/IndyPOS.Windows.Forms/UI/Inventory/UpdateInventoryProductForm.cs` — call adjust with the pending delta
- `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubInventoryProductServiceTests.cs` — update deliberately
- `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs` — replace the dud adjust test

---

### Task 1: Batched stock balances in the repository

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IInventoryMovementRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/InventoryMovementRepository.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/InventoryMovementRepositoryTests.cs` (create)

**Interfaces:**
- Consumes: `StoreHubDbContext.InventoryMovements`, `IntegrationTestBase` helpers (`GetDbContext()`, `CreateTestProductAsync(initialStock:)`)
- Produces: `Task<IReadOnlyDictionary<Guid, int>> IInventoryMovementRepository.GetBalancesAsync(string storeId, CancellationToken cancellationToken = default)` — used by Task 2

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.StoreHub.IntegrationTests/InventoryMovementRepositoryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class InventoryMovementRepositoryTests : IntegrationTestBase
{
    public InventoryMovementRepositoryTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private InventoryMovementRepository CreateRepository() => new(GetDbContext());

    private async Task AddMovementAsync(string storeId, Guid productId, int delta)
    {
        var repository = CreateRepository();

        await repository.AddAsync(new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = productId,
            QuantityDelta = delta,
            Reason = "Adjustment",
            CreatedUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public async Task GetBalancesAsync_WithSeveralMovements_ShouldSumThemPerProduct()
    {
        // CreateTestProductAsync seeds one InitialStock movement of 10 on "test-store".
        var product = await CreateTestProductAsync(initialStock: 10);
        await AddMovementAsync("test-store", product.Id, -3);
        await AddMovementAsync("test-store", product.Id, 5);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[product.Id].Should()
                            .Be(12);
    }

    [Fact]
    public async Task GetBalancesAsync_WithTwoProducts_ShouldKeepThemIndependent()
    {
        var first = await CreateTestProductAsync(initialStock: 10);
        var second = await CreateTestProductAsync(initialStock: 4);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[first.Id].Should()
                          .Be(10);
        balances[second.Id].Should()
                           .Be(4);
    }

    [Fact]
    public async Task GetBalancesAsync_WithNoMovements_ShouldOmitTheProduct()
    {
        // A product with no movements is absent, so callers read it as zero rather than
        // needing a row per product. initialStock: 0 skips the seeded movement.
        var product = await CreateTestProductAsync(initialStock: 0);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances.ContainsKey(product.Id).Should()
                                        .BeFalse();
    }

    [Fact]
    public async Task GetBalancesAsync_WithAnotherStoresMovements_ShouldExcludeThem()
    {
        var product = await CreateTestProductAsync(initialStock: 10);
        await AddMovementAsync("some-other-store", product.Id, 999);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[product.Id].Should()
                            .Be(10);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "InventoryMovementRepositoryTests"`
Expected: BUILD FAILURE — `'IInventoryMovementRepository' does not contain a definition for 'GetBalancesAsync'`.

- [ ] **Step 3: Add the interface method**

In `IInventoryMovementRepository.cs`, after `GetCurrentBalanceAsync`:

```csharp
    /// <summary>
    /// Gets current stock for every product in a store that has movements, as
    /// SUM(QuantityDelta) grouped by product. One query, not one per product.
    /// A product with no movements is absent from the dictionary — read it as zero.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetBalancesAsync(
        string storeId,
        CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Implement it**

In `InventoryMovementRepository.cs`, after `GetCurrentBalanceAsync`:

```csharp
    public async Task<IReadOnlyDictionary<Guid, int>> GetBalancesAsync(
        string storeId,
        CancellationToken cancellationToken = default)
    {
        var balances = await _dbContext.InventoryMovements
            .AsNoTracking()
            .Where(m => m.StoreId == storeId)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Balance = g.Sum(m => m.QuantityDelta) })
            .ToListAsync(cancellationToken);

        return balances.ToDictionary(b => b.ProductId, b => b.Balance);
    }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "InventoryMovementRepositoryTests"`
Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IInventoryMovementRepository.cs \
        src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/InventoryMovementRepository.cs \
        tests/IndyPOS.StoreHub.IntegrationTests/InventoryMovementRepositoryTests.cs
git commit -m "feat(inventory): add batched per-product stock balances"
```

---

### Task 2: `GET /products/stock`

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/ProductStockDto.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQuery.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQueryHandler.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (registration near line 91; endpoint after the `/products` map at line 333)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductStockEndpointTests.cs` (create)

**Interfaces:**
- Consumes: `IInventoryMovementRepository.GetBalancesAsync` (Task 1), `IStoreIdentityService.StoreId`
- Produces: `GET /products/stock` and `GET /products/stock?productId={guid}` returning `ProductStockDto[]`; `public record ProductStockDto(Guid ProductId, int Quantity)` — used by Tasks 4 and 5

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductStockEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for GET /products/stock — the read path that makes migrated
/// stock visible at the till.
/// </summary>
[Collection("Integration")]
public class ProductStockEndpointTests : IntegrationTestBase
{
    public ProductStockEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProductStock_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync("/products/stock");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductStock_AsCashier_ShouldReturnTheProductBalance()
    {
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(initialStock: 7);

        var response = await Client.GetAsync("/products/stock");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .Contain(s => s.ProductId == product.Id && s.Quantity == 7);
    }

    [Fact]
    public async Task GetProductStock_WithProductIdFilter_ShouldReturnOnlyThatProduct()
    {
        await AuthenticateAsCashierAsync();
        var wanted = await CreateTestProductAsync(initialStock: 7);
        var other = await CreateTestProductAsync(initialStock: 3);

        var response = await Client.GetAsync($"/products/stock?productId={wanted.Id}");

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .ContainSingle(s => s.ProductId == wanted.Id && s.Quantity == 7);
        stock.Should()
             .NotContain(s => s.ProductId == other.Id);
    }

    [Fact]
    public async Task GetProductStock_ForAProductWithNoMovements_ShouldOmitIt()
    {
        // Absent means zero. The client fills the gap so the wire stays small on a
        // 10,000-product store.
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(initialStock: 0);

        var response = await Client.GetAsync("/products/stock");

        var stock = await response.Content.ReadFromJsonAsync<List<ProductStockDto>>(JsonOptions);
        stock.Should()
             .NotContain(s => s.ProductId == product.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "ProductStockEndpointTests"`
Expected: BUILD FAILURE — namespace `IndyPOS.Application.UseCases.StoreHub.Products.GetStock` not found.

- [ ] **Step 3: Create the DTO and query**

`src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/ProductStockDto.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

/// <summary>
/// Current stock for one product, as SUM(InventoryMovement.QuantityDelta).
/// Deliberately not part of ProductDto: ProductDto is cached for the whole session
/// by the POS, and a cached quantity would be stale after the first sale.
/// </summary>
public record ProductStockDto(Guid ProductId, int Quantity);
```

`src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

/// <summary>
/// Current stock for this store. Supply ProductId to narrow it to one product.
/// </summary>
public record GetProductStockQuery(Guid? ProductId = null)
    : IQuery<IReadOnlyList<ProductStockDto>>;
```

- [ ] **Step 4: Create the handler**

`src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/GetProductStockQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

public class GetProductStockQueryHandler
    : IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>>
{
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IStoreIdentityService _storeIdentityService;

    public GetProductStockQueryHandler(
        IInventoryMovementRepository movementRepository,
        IStoreIdentityService storeIdentityService)
    {
        _movementRepository = movementRepository;
        _storeIdentityService = storeIdentityService;
    }

    public async Task<IReadOnlyList<ProductStockDto>> HandleAsync(
        GetProductStockQuery query,
        CancellationToken cancellationToken = default)
    {
        var balances = await _movementRepository.GetBalancesAsync(
            _storeIdentityService.StoreId, cancellationToken);

        if (query.ProductId is { } productId)
        {
            return balances.TryGetValue(productId, out var quantity)
                ? [new ProductStockDto(productId, quantity)]
                : [];
        }

        return balances.Select(b => new ProductStockDto(b.Key, b.Value))
                       .ToList();
    }
}
```

- [ ] **Step 5: Register the handler and map the endpoint**

In `src/IndyPOS.StoreHub/Program.cs`, add the using beside the other product usings (near line 15):

```csharp
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
```

Add the registration immediately after the `GetProductsQuery` line (line 91):

```csharp
builder.Services.AddTransient<IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>>, GetProductStockQueryHandler>();
```

Add the endpoint immediately after the `/products` endpoint's closing `.RequireAuthorization("CanReadProducts");` (line 333). It must come after `/products` but its literal path cannot collide, so ordering is for readability only:

```csharp
// Current stock per product. Separate from /products on purpose: the POS caches
// products for the session, and a quantity on that record would go stale at the
// first sale on either terminal.
app.MapGet("/products/stock", async (
    IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>> handler,
    Guid? productId,
    CancellationToken cancellationToken) =>
{
    var stock = await handler.HandleAsync(new GetProductStockQuery(productId), cancellationToken);
    return Results.Ok(stock);
}).RequireAuthorization("CanReadProducts");
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "ProductStockEndpointTests"`
Expected: PASS, 4 tests.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Products/GetStock/ \
        src/IndyPOS.StoreHub/Program.cs \
        tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductStockEndpointTests.cs
git commit -m "feat(storehub): add GET /products/stock"
```

---

### Task 3: Adjust quantity by delta, not target

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustQuantityRequest.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommand.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustProductQuantityCommandHandler.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/AdjustQuantityResponse.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs:482-497`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs:257-276` (replace)

**Interfaces:**
- Consumes: `IInventoryMovementRepository.AddAsync` / `GetCurrentBalanceAsync`, `IProductRepository.GetByIdAsync`
- Produces: `AdjustQuantityRequest(int Delta, string? Reason = null)`; `AdjustQuantityResponse(Guid ProductId, int Quantity)`; `POST /products/{id}/adjust-quantity` returning that response, `400` on a zero delta — used by Task 4

**Why this change:** the current handler computes `delta = target - currentBalance` server-side. Load 10 in the form, another terminal sells 3, save 12 → the server writes +5 and the sale is silently absorbed. A delta cannot do that. The existing endpoint test at line 257 posts `quantityDelta`, which binds to nothing, so `TargetQuantity` defaults to `0` and the call *zeroes* the stock — and the test asserts only `200 OK`. Replace it, do not repair it.

- [ ] **Step 1: Write the failing tests**

In `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs`, delete `AdjustQuantity_AsManager_ReturnsSuccess` (lines 257-276 including its `[Fact]`) and put these in its place:

```csharp
    [Fact]
    public async Task AdjustQuantity_AsManager_ShouldApplyTheDeltaToTheBalance()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        // Act
        var response = await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 50,
            reason = "Restock"
        });

        // Assert
        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AdjustQuantityResponse>(JsonOptions);
        result!.Quantity.Should()
                        .Be(150);

        (await GetProductStockAsync(product.Id)).Should()
                                                .Be(150);
    }

    [Fact]
    public async Task AdjustQuantity_WithASaleInBetween_ShouldNotSwallowTheSale()
    {
        // The reason this endpoint takes a delta rather than a target quantity. The
        // operator sees 100 and restocks by 50; meanwhile the other till sells 30. The
        // answer is 120. A target-based endpoint would write 150 and lose the sale.
        // If anyone reverts to target semantics, this test must fail.
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            db.InventoryMovements.Add(new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = "test-store",
                ProductId = product.Id,
                QuantityDelta = -30,
                Reason = "Sale",
                CreatedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 50,
            reason = "Restock"
        });

        (await GetProductStockAsync(product.Id)).Should()
                                                .Be(120);
    }

    [Fact]
    public async Task AdjustQuantity_WithAZeroDelta_ShouldReturnBadRequest()
    {
        // Rejected at the boundary rather than silently ignored, so a UI bug that sends
        // a no-op adjustment is visible instead of looking like it worked.
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        var response = await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 0,
            reason = "Restock"
        });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }
```

Add these usings at the top of the file if absent:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "AdjustQuantity"`
Expected: BUILD FAILURE — `AdjustQuantityResponse` does not exist.

- [ ] **Step 3: Change the request, command, and response records**

`AdjustQuantityRequest.cs` — replace the record:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Restock or write-off by a signed amount. A delta, not a target quantity: a target
/// computed against a balance read moments earlier silently absorbs any sale that lands
/// in between.
/// </summary>
public record AdjustQuantityRequest(int Delta, string? Reason = null);
```

`AdjustProductQuantityCommand.cs` — replace the record:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Command to adjust product quantity via inventory movement.
/// Writes Delta straight through; it is never derived from a balance read.
/// </summary>
public record AdjustProductQuantityCommand : ICommand<int>
{
    public required Guid ProductId { get; init; }
    public required int Delta { get; init; }
    public string? Reason { get; init; }
}
```

Create `AdjustQuantityResponse.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Result of an adjustment: the product and its balance after the movement was written.
/// The endpoint used to shape this anonymously while the client deserialized it as a
/// ProductDto, so every field of that DTO came back empty.
/// </summary>
public record AdjustQuantityResponse(Guid ProductId, int Quantity);
```

- [ ] **Step 4: Rewrite the handler body**

In `AdjustProductQuantityCommandHandler.cs`, replace `HandleAsync` entirely:

```csharp
    public async Task<int> HandleAsync(
        AdjustProductQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Delta == 0)
        {
            throw new ArgumentException("Delta must not be zero.", nameof(command));
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {command.ProductId} not found");
        }

        var storeId = _storeIdentityService.StoreId;

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = command.ProductId,
            QuantityDelta = command.Delta,
            Reason = "Adjustment",
            Note = command.Reason ?? "Manual adjustment",
            CreatedUtc = DateTime.UtcNow
        };

        await _movementRepository.AddAsync(movement, cancellationToken);

        // Read back only to report the result. The balance no longer decides what gets
        // written, which is the whole point of taking a delta.
        var newBalance = await _movementRepository.GetCurrentBalanceAsync(
            storeId, command.ProductId, cancellationToken);

        _logger.LogInformation(
            "Adjusted product {ProductId} by {Delta} (new balance {Balance})",
            command.ProductId, command.Delta, newBalance);

        return newBalance;
    }
```

- [ ] **Step 5: Update the endpoint**

In `src/IndyPOS.StoreHub/Program.cs`, replace lines 481-497:

```csharp
// Adjust product quantity by a signed delta
app.MapPost("/products/{id:guid}/adjust-quantity", async (
    ICommandHandler<AdjustProductQuantityCommand, int> handler,
    Guid id,
    AdjustQuantityRequest request,
    CancellationToken cancellationToken) =>
{
    if (request.Delta == 0)
    {
        return Results.BadRequest(new { error = "Delta must not be zero." });
    }

    var command = new AdjustProductQuantityCommand
    {
        ProductId = id,
        Delta = request.Delta,
        Reason = request.Reason
    };

    var newBalance = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(new AdjustQuantityResponse(id, newBalance));
}).RequireAuthorization("CanAdjustInventory");
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "AdjustQuantity"`
Expected: PASS, 3 tests. (The Application.Tests suite is still broken at this point — Task 4 fixes it.)

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Products/AdjustQuantity/ \
        src/IndyPOS.StoreHub/Program.cs \
        tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ProductsEndpointTests.cs
git commit -m "fix(inventory): adjust stock by delta so a concurrent sale is not swallowed"
```

---

### Task 4: Client plumbing, and real stock in the inventory list

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs:36-71`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs:184-198`
- Modify: `src/IndyPOS.Application/Common/Interfaces/IInventoryProductService.cs:29` and `:83-95`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubInventoryProductService.cs:69-240`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubInventoryProductServiceTests.cs`

**Interfaces:**
- Consumes: `GET /products/stock` (Task 2), `AdjustQuantityRequest` / `AdjustQuantityResponse` (Task 3)
- Produces: `Task<IReadOnlyDictionary<Guid, int>> IStoreHubClient.GetProductStockAsync(Guid? productId = null, CancellationToken cancellationToken = default)`; `Task<AdjustQuantityResponse> IStoreHubClient.AdjustProductQuantityAsync(...)`; `IInventoryProductService.AdjustQuantityAsync(Guid productId, int delta, string reason, ...)`; `UpdateInventoryProductRequest` **without** `QuantityInStock` — used by Task 5

**Why one task:** changing `AdjustProductQuantityAsync`'s return type breaks its only caller,
`StoreHubInventoryProductService`. Splitting the client change from the caller change would put a
commit on the branch that does not build. Every commit here builds.

All five read methods on `IInventoryProductService` are called only from `InventoryPanel` (verified:
`InventoryPanel.cs:216, 222, 345, 350, 355`). `SalePanel` does not use this service at all, so
enriching every read path adds no I/O to the barcode-scan hot path.

- [ ] **Step 1: Update the client interface**

In `IStoreHubClient.cs`, add after `GetProductsAsync` (line 40):

```csharp
    /// <summary>
    /// Get current stock for this store, keyed by product id. Pass productId to narrow
    /// it to one product. Products with no movements are absent — read them as zero.
    /// Requires authentication.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetProductStockAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default);
```

Replace the `AdjustProductQuantityAsync` declaration (lines 64-71):

```csharp
    /// <summary>
    /// Adjust product stock by a signed delta via inventory movement.
    /// Returns the balance after the movement.
    /// Requires authentication and InventoryAdjust capability.
    /// </summary>
    Task<AdjustQuantityResponse> AdjustProductQuantityAsync(
        Guid productId,
        AdjustQuantityRequest request,
        CancellationToken cancellationToken = default);
```

Add the using at the top if absent:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
```

- [ ] **Step 2: Implement both in the HTTP client**

In `StoreHubHttpClient.cs`, add the using beside the other product usings:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
```

Add after `GetProductsAsync` (after line 147):

```csharp
    public async Task<IReadOnlyDictionary<Guid, int>> GetProductStockAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var url = productId is { } id ? $"/products/stock?productId={id}" : "/products/stock";

        var stock = await SendAuthenticatedAsync<IReadOnlyList<ProductStockDto>>(
            HttpMethod.Get, url, content: null, cancellationToken);

        _logger.LogDebug("Fetched stock for {Count} products from StoreHub", stock?.Count ?? 0);

        return stock?.ToDictionary(s => s.ProductId, s => s.Quantity)
               ?? new Dictionary<Guid, int>();
    }
```

Replace `AdjustProductQuantityAsync` (lines 184-198):

```csharp
    public async Task<AdjustQuantityResponse> AdjustProductQuantityAsync(
        Guid productId,
        AdjustQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting stock for product: {Id}, Delta: {Delta}",
            productId, request.Delta);

        var result = await SendAuthenticatedAsync<AdjustQuantityResponse>(
            HttpMethod.Post, $"/products/{productId}/adjust-quantity", request, cancellationToken);

        _logger.LogInformation("Product stock adjusted. Id: {Id}, Delta: {Delta}, Balance: {Balance}",
            productId, request.Delta, result.Quantity);

        return result;
    }
```

- [ ] **Step 3: Build to find every broken call site**

Run: `dotnet build IndyPOS.sln`
Expected: FAIL. `StoreHubInventoryProductService.AdjustQuantityAsync` (line 129-140) and the Application test at line 178 no longer compile. Both are fixed in the steps below — do not commit until the build is green again.

- [ ] **Step 4: Update the existing Application test deliberately**

In `StoreHubInventoryProductServiceTests.cs`, replace `AdjustQuantityAsync_ShouldCallStoreHubClient_AndUpdateCache` (lines 177-212) with:

```csharp
    [Fact]
    public async Task AdjustQuantityAsync_ShouldSendTheDelta_AndReturnTheNewBalance()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var delta = 50;
        var reason = "Manual adjustment";

        var cachedProduct = new ProductDto(
            Id: productId,
            Barcode: "1234567890123",
            Name: "Test Product",
            Description: "Test Description",
            Category: "เครื่องดื่ม",
            Brand: null,
            Manufacturer: null,
            UnitPrice: 100m,
            GroupPrice: null,
            GroupPriceQuantity: null,
            IsActive: true);

        _productCacheServiceMock.Setup(x => x.GetById(productId))
                                .Returns(cachedProduct);

        _storeHubClientMock.Setup(x => x.AdjustProductQuantityAsync(
                productId,
                It.Is<AdjustQuantityRequest>(r => r.Delta == delta && r.Reason == reason),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdjustQuantityResponse(productId, 150));

        // Act
        var result = await _sut.AdjustQuantityAsync(productId, delta, reason);

        // Assert
        result.Id.Should()
                 .Be(productId);
        result.QuantityInStock.Should()
                              .Be(150);
    }
```

Add the using at the top if absent:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
```

- [ ] **Step 5: Write the remaining failing tests**

Add to `StoreHubInventoryProductServiceTests.cs`:

```csharp
    [Fact]
    public async Task GetAllAsync_ShouldFillQuantityInStockFromStoreHub()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = ProductWithId(productId);

        _productCacheServiceMock.Setup(x => x.GetAll())
                                .Returns(new List<ProductDto> { product });
        _storeHubClientMock.Setup(x => x.GetProductStockAsync(null, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new Dictionary<Guid, int> { [productId] = 42 });

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Single()
              .QuantityInStock.Should()
                              .Be(42);
    }

    [Fact]
    public async Task GetAllAsync_ForAProductWithNoMovements_ShouldReportZero()
    {
        // Absent from the stock dictionary means genuinely zero, not "unknown".
        // Before this change every product reported 0 because the field was hardcoded.
        var product = ProductWithId(Guid.NewGuid());

        _productCacheServiceMock.Setup(x => x.GetAll())
                                .Returns(new List<ProductDto> { product });
        _storeHubClientMock.Setup(x => x.GetProductStockAsync(null, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync();

        result.Single()
              .QuantityInStock.Should()
                              .Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFetchStockOnce_NotPerProduct()
    {
        // One GROUP BY for the whole store. A per-product fetch would be 10,000 round
        // trips on the largest store.
        _productCacheServiceMock.Setup(x => x.GetAll())
                                .Returns(new List<ProductDto>
                                {
                                    ProductWithId(Guid.NewGuid()),
                                    ProductWithId(Guid.NewGuid()),
                                    ProductWithId(Guid.NewGuid())
                                });
        _storeHubClientMock.Setup(x => x.GetProductStockAsync(null, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new Dictionary<Guid, int>());

        await _sut.GetAllAsync();

        _storeHubClientMock.Verify(
            x => x.GetProductStockAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProductDto ProductWithId(Guid id) => new(
        Id: id,
        Barcode: "1234567890123",
        Name: "Test Product",
        Description: "Test Description",
        Category: "เครื่องดื่ม",
        Brand: null,
        Manufacturer: null,
        UnitPrice: 100m,
        GroupPrice: null,
        GroupPriceQuantity: null,
        IsActive: true);
```

- [ ] **Step 6: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "StoreHubInventoryProductServiceTests"`
Expected: BUILD FAILURE (the service still calls the old client signature).

- [ ] **Step 7: Update the service interface and request record**

In `IInventoryProductService.cs`, replace the `AdjustQuantityAsync` declaration (line 26-29):

```csharp
    /// <summary>
    /// Adjust product stock by a signed delta via inventory movement.
    /// Returns the product with its balance after the movement.
    /// </summary>
    Task<InventoryProductDto> AdjustQuantityAsync(Guid productId, int delta, string reason, CancellationToken cancellationToken = default);
```

In the same file, delete this line from `UpdateInventoryProductRequest` (line 92):

```csharp
    public int QuantityInStock { get; init; }
```

Leave `CreateInventoryProductRequest.QuantityInStock` alone — that one is real, feeding `CreateProductCommand.InitialQuantity`.

- [ ] **Step 8: Rewrite the service's read and adjust paths**

In `StoreHubInventoryProductService.cs`:

Change `UpdateAsync`'s return (line 103) to fetch this product's real balance:

```csharp
        var stock = await _storeHubClient.GetProductStockAsync(result.Id, cancellationToken);

        return MapToInventoryProductDto(result, request.Category, isTrackable: true, StockFor(stock, result.Id));
```

Replace `AdjustQuantityAsync` (lines 121-141) entirely:

```csharp
    public async Task<InventoryProductDto> AdjustQuantityAsync(
        Guid productId,
        int delta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adjusting stock for product: {Id}, Delta: {Delta}", productId, delta);

        var request = new AdjustQuantityRequest(delta, reason);
        var response = await _storeHubClient.AdjustProductQuantityAsync(productId, request, cancellationToken);

        // Nothing on the cached ProductDto changed - stock deliberately does not live
        // there - so there is no cache entry to refresh, only a UI refresh to trigger.
        _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(productId);

        _logger.LogInformation("Product stock adjusted: {Id}, Delta: {Delta}, Balance: {Balance}",
            productId, delta, response.Quantity);

        var product = _productCacheService.GetById(productId)
                      ?? throw new KeyNotFoundException($"Product not found in cache: {productId}");

        return MapToInventoryProductDto(product, product.Category, isTrackable: true, response.Quantity);
    }
```

Replace the four read methods (lines 154-214):

```csharp
    public async Task<InventoryProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = _productCacheService.GetByBarcode(barcode);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product not found with barcode: {barcode}");
        }

        var stock = await _storeHubClient.GetProductStockAsync(product.Id, cancellationToken);

        return MapToInventoryProductDto(product, product.Category, isTrackable: true, StockFor(stock, product.Id));
    }

    public async Task<IReadOnlyList<InventoryProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> GetByCategoryAsync(
        string categoryCode,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            // Ordinal to match ProductCategoryRepository.GetByCodeAsync; codes come from constants.
            .Where(p => string.Equals(p.Category, categoryCode, StringComparison.Ordinal))
            .Select(p => MapToInventoryProductDto(p, categoryCode, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> SearchByDescriptionAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.Search(keyword)
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryProductDto>> SearchByBrandAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        var stock = await _storeHubClient.GetProductStockAsync(cancellationToken: cancellationToken);

        return _productCacheService.GetAll()
            .Where(p => p.Brand?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
            .Select(p => MapToInventoryProductDto(p, p.Category, isTrackable: true, StockFor(stock, p.Id)))
            .ToList();
    }
```

Replace the mapper's signature and the hardcoded zero (lines 218-233). The parameter is now required, so nothing can silently default to zero again:

```csharp
    private InventoryProductDto MapToInventoryProductDto(
        ProductDto product,
        string? categoryCode,
        bool isTrackable,
        int quantityInStock)
    {
        return new InventoryProductDto
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Name,
            Manufacturer = product.Manufacturer ?? string.Empty,
            Brand = product.Brand ?? string.Empty,
            Category = categoryCode ?? string.Empty,
            UnitPrice = product.UnitPrice,
            QuantityInStock = quantityInStock,
```

(leave the remaining initialisers unchanged)

Add the helper at the end of the `#region Private Helpers` block:

```csharp
    /// <summary>
    /// A product absent from the balances has no movements, which is genuinely zero.
    /// </summary>
    private static int StockFor(IReadOnlyDictionary<Guid, int> stock, Guid productId)
        => stock.TryGetValue(productId, out var quantity) ? quantity : 0;
```

- [ ] **Step 9: Build and run the tests to verify they pass**

Run: `dotnet build IndyPOS.sln` then `dotnet test tests/IndyPOS.Application.Tests --filter "StoreHubInventoryProductServiceTests"`
Expected: 0 build errors, tests PASS. The build must be green before committing. If `CreateAsync`'s call to `MapToInventoryProductDto` fails to compile, pass `request.QuantityInStock` as the new fourth argument — the create path's typed quantity is real.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs \
        src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs \
        src/IndyPOS.Application/Common/Interfaces/IInventoryProductService.cs \
        src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubInventoryProductService.cs \
        tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubInventoryProductServiceTests.cs
git commit -m "feat(inventory): show real stock in the inventory list"
```

---

### Task 5: Make the edit form actually restock

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Inventory/PendingStockAdjustment.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Inventory/UpdateInventoryProductForm.cs:130-170, 196-224`
- Test: `tests/IndyPOS.Windows.Forms.Tests/UI/Inventory/PendingStockAdjustmentTests.cs` (create)

**Interfaces:**
- Consumes: `IInventoryProductService.AdjustQuantityAsync(Guid, int delta, string reason, ...)` (Task 4)
- Produces: nothing downstream

**Note on coverage:** the repo has no harness for instantiating WinForms forms (`IndyPOS.Windows.Forms.Tests` only covers the error-reporting helpers), so the accumulate-and-decide logic is extracted into `PendingStockAdjustment` and unit-tested there, leaving the form a thin caller. The spec's test table said this would be an Application.Tests case; this is the same coverage in the suite that can actually host it. Everything else in this task is covered by the manual run in Task 6 — plus a compile-time guarantee: `UpdateInventoryProductRequest.QuantityInStock` is gone, so the old silent path cannot come back.

- [ ] **Step 1: Write the failing tests**

Create `tests/IndyPOS.Windows.Forms.Tests/UI/Inventory/PendingStockAdjustmentTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Inventory;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Inventory;

public class PendingStockAdjustmentTests
{
    [Fact]
    public void Delta_WithNoChanges_ShouldBeZero()
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Delta.Should()
                        .Be(0);
        adjustment.HasChange.Should()
                            .BeFalse();
    }

    [Fact]
    public void Increase_ShouldRaiseTheDisplayedQuantityAndTheDelta()
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(5);

        adjustment.DisplayedQuantity.Should()
                                    .Be(15);
        adjustment.Delta.Should()
                        .Be(5);
    }

    [Theory]
    [InlineData(3, 2, 5)]
    [InlineData(10, -4, 6)]
    public void Increase_AppliedTwice_ShouldAccumulate(int first, int second, int expectedDelta)
    {
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(first);
        adjustment.Increase(second);

        adjustment.Delta.Should()
                        .Be(expectedDelta);
    }

    [Fact]
    public void Increase_ThenBackToTheStartingQuantity_ShouldReportNoChange()
    {
        // The form must not send a zero-delta adjustment: the endpoint rejects it, and
        // an empty movement row would be noise in the audit trail.
        var adjustment = new PendingStockAdjustment(startingQuantity: 10);

        adjustment.Increase(5);
        adjustment.Increase(-5);

        adjustment.HasChange.Should()
                            .BeFalse();
    }

    [Fact]
    public void Increase_BelowZero_ShouldBeAllowed()
    {
        // Nothing clamps. Negative stock is real and must stay visible - hiding it is
        // what defect 14b was.
        var adjustment = new PendingStockAdjustment(startingQuantity: 2);

        adjustment.Increase(-5);

        adjustment.DisplayedQuantity.Should()
                                    .Be(-3);
        adjustment.Delta.Should()
                        .Be(-5);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "PendingStockAdjustmentTests"`
Expected: BUILD FAILURE — `PendingStockAdjustment` does not exist.

- [ ] **Step 3: Create the accumulator**

`src/IndyPOS.Windows.Forms/UI/Inventory/PendingStockAdjustment.cs`:

```csharp
namespace IndyPOS.Windows.Forms.UI.Inventory;

/// <summary>
/// Tracks what the operator has done to a product's stock figure before saving.
/// The form shows DisplayedQuantity and sends Delta, never the target: a target computed
/// against a quantity read when the dialog opened would absorb any sale made in between.
/// </summary>
public class PendingStockAdjustment
{
    private readonly int _startingQuantity;

    public PendingStockAdjustment(int startingQuantity)
    {
        _startingQuantity = startingQuantity;
        DisplayedQuantity = startingQuantity;
    }

    public int DisplayedQuantity { get; private set; }

    public int Delta => DisplayedQuantity - _startingQuantity;

    public bool HasChange => Delta != 0;

    public void Increase(int amount) => DisplayedQuantity += amount;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Windows.Forms.Tests --filter "PendingStockAdjustmentTests"`
Expected: PASS, 6 tests (4 `[Fact]` plus the `[Theory]`'s 2 cases).

- [ ] **Step 5: Wire it into the form**

In `UpdateInventoryProductForm.cs`, add a field beside the other private fields:

```csharp
	private PendingStockAdjustment? _stockAdjustment;
```

In `PopulateProductProperties`, replace line 71:

```csharp
		_stockAdjustment = new PendingStockAdjustment(_product.QuantityInStock);
		QuantityLabel.Text = $"{_stockAdjustment.DisplayedQuantity}";
```

Replace `AdjustQuantityBy` (lines 213-224) — the label is no longer the source of truth:

```csharp
	/// <summary>
	/// Applies the entered amount to the running stock figure. Both buttons share this
	/// path so the validation gate cannot be omitted from one of them - the increase
	/// button used to parse the box directly, and an empty box (its initial state, since
	/// PopulateProductProperties never fills it) threw FormatException straight out of an
	/// event handler.
	/// </summary>
	private void AdjustQuantityBy(int direction)
	{
		if (_stockAdjustment is null || !ValidateQuantity())
			return;

		var amount = int.Parse(QuantityTextBox.Texts.Trim());

		_stockAdjustment.Increase(direction * amount);
		QuantityLabel.Text = $"{_stockAdjustment.DisplayedQuantity}";

		QuantityTextBox.Texts = string.Empty;
	}
```

Replace `UpdateProductButton_Click` (lines 130-147) so the adjustment is actually sent:

```csharp
	private async void UpdateProductButton_Click(object sender, EventArgs e)
	{
		if (_product is null || _stockAdjustment is null || !ValidateProductEntry())
			return;

		try
		{
			await _inventoryProductService.UpdateAsync(CreateRequestForUpdateProduct(_product));

			// Stock is a separate concern from the product record: it is a movement, not
			// a column. UpdateAsync has never carried it - the typed quantity used to be
			// parsed and silently dropped.
			if (_stockAdjustment.HasChange)
			{
				await _inventoryProductService.AdjustQuantityAsync(
					_product.Id, _stockAdjustment.Delta, "แก้ไขจำนวนสินค้า");
			}

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า");
		}
	}
```

Delete this line from `CreateRequestForUpdateProduct` (line 162) — the property no longer exists on the request:

```csharp
			QuantityInStock = int.Parse(QuantityLabel.Text.Trim()),
```

- [ ] **Step 6: Build and run the full suite**

Run: `dotnet build IndyPOS.sln` then `dotnet test IndyPOS.sln`
Expected: 0 build errors. **570 pass / 1 skip / 0 fail (571 discovered)**, up from a 551 pass /
1 skip baseline. (Task 1 adds 4, Task 2 adds 4, Task 3 is +3/−1, Task 4 adds 3 and swaps one for
one, Task 5 adds 6 — 4 `[Fact]` plus a `[Theory]` contributing 2 cases. Net +19 passing.)
If any other call site of `AdjustQuantityAsync` or `UpdateInventoryProductRequest.QuantityInStock` surfaces, fix it here rather than deferring.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Inventory/PendingStockAdjustment.cs \
        src/IndyPOS.Windows.Forms/UI/Inventory/UpdateInventoryProductForm.cs \
        tests/IndyPOS.Windows.Forms.Tests/UI/Inventory/PendingStockAdjustmentTests.cs
git commit -m "fix(inventory): send the restock instead of dropping it"
```

---

### Task 6: Verify against a real store, then record it

**Files:**
- Modify: `.claude/STATUS.md` (gitignored — do not stage)
- Modify: `.planning/indypos-overhaul/PLAN.md` (the Epic 2 "NEXT" framing and test-state table)
- Modify: `CLAUDE.md` (test counts in Quick Commands)

**Interfaces:**
- Consumes: everything above
- Produces: evidence that a migrated store shows correct stock at the till

**Why manual:** the console and UI paths have no automated coverage, and a real run has twice caught this codebase reporting success on a run that wrote nothing. Tests passing is not the finish line here.

- [ ] **Step 1: Build a migrated fixture**

```bash
docker run --rm -d -e POSTGRES_PASSWORD=dev -p 5555:5432 --name stockcheck postgres:16-alpine
dotnet run --project src/IndyPOS.MigrationTool -- \
  --sqlite .planning/indypos-overhaul/sqlite_database/MimyShop/Store.db \
  --postgres "Host=localhost;Port=5555;Database=postgres;Username=postgres;Password=dev" \
  --store-id MIMYSHOP
```

Expected: completes, reports migrated products, and prints any clamped-stock list.

- [ ] **Step 2: Confirm the API matches the database**

```bash
docker exec stockcheck psql -U postgres -c \
  "SELECT product_id, SUM(quantity_delta) FROM inventory_movement GROUP BY product_id ORDER BY 2 DESC LIMIT 5;"
```

Then start StoreHub against that database, authenticate, and `GET /products/stock`. The top five products must match the SQL exactly.

- [ ] **Step 3: Confirm the till**

Run the POS, open the inventory panel, and check that `จำนวนในคลัง` shows those same numbers rather than 0 for every row. **This is the check the whole plan exists for** — defect 14's fix has never been visible on a screen.

- [ ] **Step 4: Confirm a restock round-trips**

Open a product in the edit form, press increase with an amount of 5, save, reopen the panel. The figure must be 5 higher, and a new `Adjustment` row must exist:

```bash
docker exec stockcheck psql -U postgres -c \
  "SELECT quantity_delta, reason, note FROM inventory_movement WHERE reason = 'Adjustment';"
```

Expected: exactly one row, `quantity_delta = 5`.

- [ ] **Step 5: Tear down**

```bash
docker rm -f stockcheck
```

- [ ] **Step 6: Update the docs**

- `PLAN.md`: in the Epic 2 section, record that stock is now observable at the till and that defect 14's fix is confirmed on a real migrated store. Update the test-state table's per-suite counts and solution total from the Task 6 run.
- `CLAUDE.md`: update the test counts in Quick Commands to match.
- `.claude/STATUS.md`: replace the 🔴 "Stock is not observable at the till" item under **NEXT** with the outcome, and promote the next item (defect 7b, or defect 5 now that Epic 1 has shipped). **Do not stage this file** — it is gitignored and the repo is public.

- [ ] **Step 7: Commit**

```bash
git add .planning/indypos-overhaul/PLAN.md CLAUDE.md
git commit -m "docs: record stock visibility at the till and refresh test counts"
```

---

## Definition of done

- `dotnet test IndyPOS.sln` green with Docker up and the real store `.db` files present.
- `GET /products/stock` matches `SELECT product_id, SUM(quantity_delta) ... GROUP BY product_id` on a real migrated store.
- `InventoryPanel` shows those numbers; `SalePanel` is unchanged and shows none.
- A restock through the edit form writes exactly one `Adjustment` movement with the operator's delta.
- No EF migration was added.
- Defect 7b is untouched and still documented as open — non-trackable products still show a stock figure.
