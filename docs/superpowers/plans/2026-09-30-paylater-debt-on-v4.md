# PayLater Debts on v4 Sales Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A PayLater sale rung on v4 creates its debt atomically with the sale. The server refuses a sale that would create an unusable debt or mix credit with another payment.

**Architecture:** The payment rules move into a small pure class, `SalePaymentRules`, which throws a new `SaleValidationException`. `CompleteSaleCommandHandler` calls it before anything is saved, then attaches one `PayLater` to its `Payment` through the existing 1:1 navigation. EF saves the debt in the same `SaveChangesAsync` as the invoice, so the repository does not change. `/sales/complete` maps the exception to a Thai 400. The till refuses the ลงบัญชี button once the sale already has a payment.

**Tech Stack:** C# .NET 10, EF Core + Npgsql, xUnit, FluentAssertions, Moq + AutoFixture (`[CustomAutoData]`), Testcontainers (StoreHub integration), Windows Forms.

**Spec:** `docs/superpowers/specs/2026-09-30-paylater-debt-on-v4-design.md`

## Global Constraints

- **No schema change and no migration.** The forward-only gate is unaffected.
- **Nothing is saved for a refused sale:** "A validation failure throws before `CompleteSaleAsync`, so a refused sale writes nothing: no invoice, no payment, no debt, no stock movement, no outbox event." (spec §4.3)
- **PayLater is recognised ignoring case:** the handler "accepts `paylater` as valid … and stores the caller's spelling" (spec §4.1).
- **A PayLater sale is paid wholly on credit:** never mixed with another method, never on a refund (spec §2, §3).
- **The Thai messages are verbatim** from spec §4.2:
  - `ช่องทางชำระเงิน '{method}' ใช้กับร้านนี้ไม่ได้`
  - `กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี`
  - `ชื่อลูกค้ายาวเกิน 500 ตัวอักษร`
  - `ยอดลงบัญชีต้องมากกว่า 0`
  - `การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น`
  - `ไม่สามารถลงบัญชีบิลคืนสินค้าได้`
  - `ยอดลงบัญชีต้องเท่ากับยอดบิล`
- **The 400 body is `{ error }`,** the shape the `/cash` routes use.
- **Test style:** `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test, negative cases first, and FluentAssertions chains with the dots aligned.
- **Every test must be able to fail:** run each RED step and read the failure before writing the fix.

## Review Focus

The inputs the spec implies but its test list does not name, most likely first. Each has a test in its owning task:

1. **A Note of only spaces around a real name** (`"  ลุงสมชาย  "`). Expected: accepted, and stored trimmed. → Task 2 (`…WithAPaddedCustomerName_StoresItTrimmed`).
2. **The 500-character limit counted after trimming.** A name of 500 characters plus padding must pass, because the stored value is the trimmed one. → Task 1 (`EnsureValid_WithA500CharacterNameAndPadding_DoesNotThrow`).
3. **An empty sale with a PayLater payment** (no lines, total 0). Expected: refused as a refund or empty bill, not saved as a ฿0 debt. → Task 1 (`EnsureValid_WithPayLaterOnAnEmptySale_Throws`).
4. **The integration database is shared and never reset**, so a test that reads totals or counts must compare before and after. → Task 3 (both summary and no-invoice tests are written that way).
5. **Two PayLater payments that add up to the total.** Expected: refused as a mix. → Task 1 (`EnsureValid_WithTwoPayLaterPayments_Throws`).

---

## File Structure

```
src/IndyPOS.Application/
  Common/Exceptions/SaleValidationException.cs                  NEW  -> 400
  UseCases/StoreHub/Sales/Complete/SalePaymentRules.cs          NEW  pure payment rules
  UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs MOD  rules first; attach the debt

src/IndyPOS.StoreHub/Program.cs                                 MOD  /sales/complete maps the exception
src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs       MOD  refuse ลงบัญชี after a payment

tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/
  SalePaymentRulesTests.cs                                      NEW
  CompleteSaleCommandHandlerPayLaterTests.cs                    NEW
  CompleteSaleCommandHandlerTests.cs                            MOD  the not-offerable test's exception
tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/
  PayLaterSaleEndpointTests.cs                                  NEW

CLAUDE.md, ONBOARDING.md                                        MOD  suite counts (Task 4)
```

---

### Task 1: The payment rules, refused before anything is saved

**Files:**
- Create: `src/IndyPOS.Application/Common/Exceptions/SaleValidationException.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/SalePaymentRules.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs:42-52` (the offerable check) and `:61` (`TotalAmount`)
- Modify: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerTests.cs:370`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/SalePaymentRulesTests.cs`

**Interfaces:**
- Consumes: `SalePaymentRequest(string Method, decimal Amount, string? Note = null)`, `PaymentMethodCodes.PayLater`.
- Produces:
  - `SaleValidationException(string message)` in namespace `IndyPOS.Application.Common.Exceptions`.
  - `SalePaymentRules.EnsureValid(IReadOnlyList<SalePaymentRequest> payments, decimal invoiceTotal, IReadOnlySet<string> offerableCodes)`.
  - `SalePaymentRules.IsPayLater(string method) : bool`.
  - `SalePaymentRules.MaxCustomerNameLength = 500`.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/SalePaymentRulesTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// Spec 2026-09-30 §4.2: what a sale's payments must satisfy before anything is saved. A PayLater
/// sale is paid wholly on credit, by a named customer, and never on a refund.
/// </summary>
public class SalePaymentRulesTests
{
    private const decimal Total = 350m;
    private const string CustomerName = "ลุงสมชาย";
    private const string DisabledCampaign = PaymentMethodCodes.M33WeLove;

    private static readonly IReadOnlySet<string> Offerable =
        new HashSet<string>([PaymentMethodCodes.Cash, PaymentMethodCodes.PayLater], StringComparer.OrdinalIgnoreCase);

    private static SalePaymentRequest Credit(decimal amount = Total, string? note = CustomerName) =>
        new(PaymentMethodCodes.PayLater, amount, note);

    private static Action Validate(decimal invoiceTotal, params SalePaymentRequest[] payments) =>
        () => SalePaymentRules.EnsureValid(payments, invoiceTotal, Offerable);

    [Fact]
    public void EnsureValid_WithAMethodNotOfferable_Throws()
    {
        Validate(Total, new SalePaymentRequest(DisabledCampaign, Total)).Should()
                                                                        .Throw<SaleValidationException>()
                                                                        .WithMessage($"*'{DisabledCampaign}'*");
    }

    [Fact]
    public void EnsureValid_WithPayLaterAndNoNote_Throws()
    {
        Validate(Total, Credit(note: null)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public void EnsureValid_WithPayLaterAndAWhitespaceNote_Throws()
    {
        Validate(Total, Credit(note: "   ")).Should()
                                            .Throw<SaleValidationException>();
    }

    [Fact]
    public void EnsureValid_WithACustomerNameOver500Characters_Throws()
    {
        var name = new string('ก', SalePaymentRules.MaxCustomerNameLength + 1);

        Validate(Total, Credit(note: name)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("ชื่อลูกค้ายาวเกิน 500 ตัวอักษร");
    }

    [Fact]
    public void EnsureValid_WithPayLaterMixedWithCash_Throws()
    {
        Validate(Total, Credit(amount: 200m), new SalePaymentRequest(PaymentMethodCodes.Cash, 150m)).Should()
                                                                                                    .Throw<SaleValidationException>()
                                                                                                    .WithMessage("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");
    }

    [Fact]
    public void EnsureValid_WithTwoPayLaterPayments_Throws()
    {
        Validate(Total, Credit(amount: 200m), Credit(amount: 150m)).Should()
                                                                   .Throw<SaleValidationException>()
                                                                   .WithMessage("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");
    }

    [Fact]
    public void EnsureValid_WithPayLaterOnARefund_Throws()
    {
        Validate(-Total, Credit(amount: Total)).Should()
                                               .Throw<SaleValidationException>()
                                               .WithMessage("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");
    }

    [Fact]
    public void EnsureValid_WithPayLaterOnAnEmptySale_Throws()
    {
        Validate(0m, Credit(amount: 0m)).Should()
                                        .Throw<SaleValidationException>()
                                        .WithMessage("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");
    }

    [Fact]
    public void EnsureValid_WithANegativePayLaterAmount_Throws()
    {
        Validate(Total, Credit(amount: -1m)).Should()
                                            .Throw<SaleValidationException>()
                                            .WithMessage("ยอดลงบัญชีต้องมากกว่า 0");
    }

    [Fact]
    public void EnsureValid_WithAZeroPayLaterAmount_Throws()
    {
        Validate(Total, Credit(amount: 0m)).Should()
                                           .Throw<SaleValidationException>()
                                           .WithMessage("ยอดลงบัญชีต้องมากกว่า 0");
    }

    [Fact]
    public void EnsureValid_WithAPayLaterAmountBelowTheTotal_Throws()
    {
        Validate(Total, Credit(amount: Total - 1m)).Should()
                                                   .Throw<SaleValidationException>()
                                                   .WithMessage("ยอดลงบัญชีต้องเท่ากับยอดบิล");
    }

    [Fact]
    public void EnsureValid_WithAPayLaterCoveringTheWholeBill_DoesNotThrow()
    {
        Validate(Total, Credit()).Should()
                                 .NotThrow();
    }

    [Fact]
    public void EnsureValid_WithALowerCasePayLaterCode_AppliesThePayLaterRules()
    {
        Validate(Total, new SalePaymentRequest("paylater", Total, Note: null)).Should()
                                                                              .Throw<SaleValidationException>()
                                                                              .WithMessage("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public void EnsureValid_WithExactly500Characters_DoesNotThrow()
    {
        var name = new string('ก', SalePaymentRules.MaxCustomerNameLength);

        Validate(Total, Credit(note: name)).Should()
                                           .NotThrow();
    }

    // The stored name is the trimmed one, so padding does not count towards the limit.
    [Fact]
    public void EnsureValid_WithA500CharacterNameAndPadding_DoesNotThrow()
    {
        var name = $"  {new string('ก', SalePaymentRules.MaxCustomerNameLength)}  ";

        Validate(Total, Credit(note: name)).Should()
                                           .NotThrow();
    }

    [Fact]
    public void EnsureValid_WithCashAndMoneyTransfer_DoesNotThrow()
    {
        var offerable = new HashSet<string>([PaymentMethodCodes.Cash, "MoneyTransfer"], StringComparer.OrdinalIgnoreCase);
        SalePaymentRequest[] payments = [new(PaymentMethodCodes.Cash, 200m), new("MoneyTransfer", 150m)];

        var act = () => SalePaymentRules.EnsureValid(payments, Total, offerable);

        act.Should()
           .NotThrow();
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SalePaymentRulesTests"`
Expected: build FAILS, because `SalePaymentRules` and `SaleValidationException` do not exist.

- [ ] **Step 3: Add the exception**

`src/IndyPOS.Application/Common/Exceptions/SaleValidationException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A sale the store must not record: its payments break a rule. The message is Thai, for the
/// cashier. /sales/complete answers it with 400.
/// </summary>
public class SaleValidationException(string message) : Exception(message);
```

- [ ] **Step 4: Add the rules**

`src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/SalePaymentRules.cs`:

```csharp
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

/// <summary>
/// What a sale's payments must satisfy before anything is saved (spec 2026-09-30 §4.2). A PayLater
/// sale is paid wholly on credit: never mixed with another method, because a mixed bill cannot be
/// split honestly between general goods and hardware, and never on a refund, which would record a
/// debt for money the store owes.
/// </summary>
public static class SalePaymentRules
{
    /// <summary>PayLater.Description's column length. Payment.Note has no limit.</summary>
    public const int MaxCustomerNameLength = 500;

    public static void EnsureValid(
        IReadOnlyList<SalePaymentRequest> payments, decimal invoiceTotal, IReadOnlySet<string> offerableCodes)
    {
        var notOfferable = payments.FirstOrDefault(p => !offerableCodes.Contains(p.Method));
        if (notOfferable is not null)
            throw new SaleValidationException($"ช่องทางชำระเงิน '{notOfferable.Method}' ใช้กับร้านนี้ไม่ได้");

        if (payments.Any(p => IsPayLater(p.Method)))
            EnsureWhollyOnCredit(payments, invoiceTotal);
    }

    /// <summary>
    /// Ignoring case: the handler accepts "paylater" as offerable and stores the caller's spelling.
    /// </summary>
    public static bool IsPayLater(string method) =>
        string.Equals(method, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase);

    private static void EnsureWhollyOnCredit(IReadOnlyList<SalePaymentRequest> payments, decimal invoiceTotal)
    {
        if (payments.Count > 1)
            throw new SaleValidationException("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น");

        EnsureNamedCustomer(payments[0].Note);

        if (invoiceTotal <= 0)
            throw new SaleValidationException("ไม่สามารถลงบัญชีบิลคืนสินค้าได้");

        if (payments[0].Amount <= 0)
            throw new SaleValidationException("ยอดลงบัญชีต้องมากกว่า 0");

        if (payments[0].Amount != invoiceTotal)
            throw new SaleValidationException("ยอดลงบัญชีต้องเท่ากับยอดบิล");
    }

    private static void EnsureNamedCustomer(string? note)
    {
        var name = note?.Trim();

        if (string.IsNullOrEmpty(name))
            throw new SaleValidationException("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");

        if (name.Length > MaxCustomerNameLength)
            throw new SaleValidationException($"ชื่อลูกค้ายาวเกิน {MaxCustomerNameLength} ตัวอักษร");
    }
}
```

The order of the checks is fixed on purpose, and each test's expected message relies on it:
1. mix;
2. name;
3. refund or empty bill;
4. amount;
5. equal to the total.

For example, `Credit(amount: 0m)` on a ฿0 bill reports the refund/empty message, not the amount one.

- [ ] **Step 5: Run the rule tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SalePaymentRulesTests"`
Expected: PASS, 16 tests.

- [ ] **Step 6: Call the rules from the handler**

In `CompleteSaleCommandHandler.cs`, add `using IndyPOS.Application.Common.Exceptions;`, then replace lines 42-52 (the comment `// Validate all payment methods are offerable…` through the closing `}` of the `if (rejected is not null)` block) with:

```csharp
        // Every payment rule runs before any lookup or save, so a refused sale writes nothing.
        var offerable = await _catalog.GetOfferableAsync(cancellationToken);
        var offerableCodes = offerable.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invoiceTotal = command.Lines.Sum(l => l.Quantity * l.UnitPrice);

        try
        {
            SalePaymentRules.EnsureValid(command.Payments, invoiceTotal, offerableCodes);
        }
        catch (SaleValidationException ex)
        {
            _logger.LogWarning("Sale refused: {Reason}, StoreType={StoreType}, UserId={UserId}",
                ex.Message, _storeIdentity.StoreType, command.UserId);
            throw;
        }
```

In the `new Invoice { … }` initializer below, replace `TotalAmount = command.Lines.Sum(l => l.Quantity * l.UnitPrice),` with `TotalAmount = invoiceTotal,`, so the total is computed once.

- [ ] **Step 7: Update the existing not-offerable test**

In `CompleteSaleCommandHandlerTests.cs:370`, change:

```csharp
        await act.Should().ThrowAsync<InvalidOperationException>();
```

to:

```csharp
        await act.Should().ThrowAsync<SaleValidationException>();
```

and add `using IndyPOS.Application.Common.Exceptions;` at the top. The test's own comment about `M33WeLove` still holds.

- [ ] **Step 8: Run the Application tests**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~Sales"`
Expected: PASS. Before Step 7, the not-offerable test failed with "Expected InvalidOperationException, but SaleValidationException was thrown". That failure is the proof the handler now uses the rules.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Application/Common/Exceptions/SaleValidationException.cs src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands
git commit -m "feat(sales): refuse a sale whose payments break a PayLater rule"
```

---

### Task 2: Attach the debt to its payment, in the same save

**Files:**
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs` (after the payments are built, ~`:118-127`)
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerPayLaterTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs` (the regression test)

**Interfaces:**
- Consumes: `SalePaymentRules.IsPayLater(string)` (Task 1); `Payment.PayLater : PayLater?`, the existing 1:1 navigation (`PayLaterConfiguration.cs:60-61`).
- Produces: one `PayLater` per PayLater payment, reachable as `payment.PayLater` on the payments passed to `ISaleRepository.CompleteSaleAsync`, and saved with them.

**Why the navigation, not a new repository parameter (deviation D1):** `SaleRepository.CompleteSaleAsync` adds each `Payment` to the context, and EF adds a payment's `PayLater` with it, in the same `SaveChangesAsync`. That gives the atomicity spec §4.1 asks for, with no signature change and no edits to the 7 existing Moq setups.

- [ ] **Step 1: Write the failing handler tests**

`tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerPayLaterTests.cs`:

```csharp
using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// A v4 credit sale created no pay_later row, so the debt was invisible, could not be repaid, and
/// made the cash drawer come out short (spec 2026-09-30 §1).
/// </summary>
public class CompleteSaleCommandHandlerPayLaterTests
{
    private const decimal Price = 350m;
    private const string CustomerName = "ลุงสมชาย";

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithARefusedSale_SavesNothing(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        Arrange(saleRepository, productRepository, catalog, out var productId);

        var act = () => sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, Note: null)));

        await act.Should()
                 .ThrowAsync<SaleValidationException>();
        saleRepository.Verify(r => r.CompleteSaleAsync(
                It.IsAny<Invoice>(), It.IsAny<IReadOnlyList<InvoiceLine>>(), It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(), It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithACashSale_AttachesNoDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.Cash, Price)));

        saved.Single().PayLater.Should()
                               .BeNull();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithAPayLaterPayment_AttachesItsDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, CustomerName)));

        var payment = saved.Single();
        payment.PayLater.Should()
                        .BeEquivalentTo(new
                        {
                            PaymentId = payment.Id,
                            payment.InvoiceId,
                            Description = CustomerName,
                            PayLaterAmount = Price,
                            PaidAmount = 0m,
                            IsCompleted = false
                        });
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithALowerCasePayLaterCode_AttachesItsDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest("paylater", Price, CustomerName)));

        saved.Single().PayLater.Should()
                               .NotBeNull();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithAPaddedCustomerName_StoresItTrimmed(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, $"  {CustomerName}  ")));

        saved.Single().PayLater!.Description.Should()
                                            .Be(CustomerName);
    }

    /// <summary>One product at <see cref="Price"/>; Cash and PayLater offerable; captures the saved payments.</summary>
    private static List<Payment> Arrange(
        Mock<ISaleRepository> saleRepository,
        Mock<IProductRepository> productRepository,
        Mock<IPaymentMethodCatalogService> catalog,
        out Guid productId)
    {
        var id = Guid.NewGuid();
        productId = id;
        var saved = new List<Payment>();

        productRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new Product { Id = id, Barcode = "P1", Name = "Nail", UnitPrice = Price, IsActive = true, IsTrackable = true });

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([
                   new PaymentMethod { Code = PaymentMethodCodes.Cash, DisplayName = "Cash", IsEnabled = true, StoreId = "STORE-001" },
                   new PaymentMethod { Code = PaymentMethodCodes.PayLater, DisplayName = "PayLater", IsEnabled = true, StoreId = "STORE-001" }
               ]);

        saleRepository.Setup(r => r.CompleteSaleAsync(
                          It.IsAny<Invoice>(), It.IsAny<IReadOnlyList<InvoiceLine>>(), It.IsAny<IReadOnlyList<Payment>>(),
                          It.IsAny<IReadOnlyList<InventoryMovement>>(), It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                      .Callback((Invoice _, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> payments,
                          IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => saved.AddRange(payments))
                      .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                          IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        return saved;
    }

    private static CompleteSaleCommand SaleOf(Guid productId, SalePaymentRequest payment) =>
        new(StoreId: "STORE-001",
            UserId: Guid.NewGuid(),
            Lines: [new SaleLineRequest(productId, Quantity: 1, UnitPrice: Price)],
            Payments: [payment]);
}
```

If `GetOfferableAsync`'s return type does not accept a collection expression, use `new List<PaymentMethod> { … }`. It is the same shape as `OfferableWith` in `CompleteSaleCommandHandlerTests.cs:29-38`.

- [ ] **Step 2: Write the failing integration regression test**

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Spec 2026-09-30: a v4 credit sale creates its debt, like a migrated v3 one. The shared database
/// is never reset, so tests find their own rows by invoice id, and compare totals before and after.
/// </summary>
[Collection("Integration")]
public class PayLaterSaleEndpointTests : IntegrationTestBase
{
    private const decimal Price = 350m;
    private const string CustomerName = "ลุงสมชาย";

    public PayLaterSaleEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    // The regression test: on development this finds no row.
    [Fact]
    public async Task CompleteSale_WithAPayLaterPayment_CreatesItsDebt()
    {
        await AuthenticateAsCashierAsync();

        var sale = await SellOnCreditAsync();

        (await CountDebtsOfAsync(sale.InvoiceId)).Should()
                                                 .Be(1);
    }

    private async Task<CompleteSaleResponse> SellOnCreditAsync(string? note = CustomerName)
    {
        var response = await Client.PostAsJsonAsync("/sales/complete", await CreditSaleAsync(note));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    private async Task<CompleteSaleRequest> CreditSaleAsync(string? note, params SalePaymentRequest[] extraPayments)
    {
        var product = await CreateTestProductAsync(unitPrice: Price, initialStock: 10);
        var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        return new CompleteSaleRequest(
            UserId: seller.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: Price)],
            Payments: [new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, note), .. extraPayments]);
    }

    private async Task<int> CountDebtsOfAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.PayLaters.CountAsync(p => p.InvoiceId == invoiceId);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSaleCommandHandlerPayLaterTests"`
Expected:
- `…AttachesItsDebt`, `…LowerCase…` and `…StoresItTrimmed` FAIL, because `PayLater` is null.
- `…RefusedSale_SavesNothing` and `…CashSale_AttachesNoDebt` PASS already. Task 1 gave the first; the second pins the cash case.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CompleteSale_WithAPayLaterPayment_CreatesItsDebt"`
Expected: FAIL with "Expected … to be 1, but found 0". **If it fails with a non-2xx status instead,** PayLater is not offerable in the test store. Stop and check the seeded catalogue: `TestStoreIdentityService` is GeneralHardware, so it should be.

- [ ] **Step 4: Attach the debt**

In `CompleteSaleCommandHandler.cs`, add at the top, with the other `using`s:

```csharp
// Inside IndyPOS.Application.UseCases.StoreHub.*, the bare name PayLater binds to the sibling
// namespace IndyPOS.Application.UseCases.StoreHub.PayLater, not to the entity.
using PayLaterDebt = IndyPOS.Domain.Entities.Core.PayLater;
```

Directly after the `var payments = command.Payments.Select(…).ToList();` statement, add:

```csharp

        // Spec 2026-09-30 §4.1: the debt rides its payment's 1:1 navigation, so EF saves it in the
        // same SaveChangesAsync as the sale. A credit sale can never exist without its debt.
        foreach (var payment in payments.Where(p => SalePaymentRules.IsPayLater(p.Method)))
            payment.PayLater = NewDebt(payment, now);
```

and, as a new private method at the end of the class:

```csharp

    /// <summary>The shape the MigrationTool writes for a v3 debt: nothing paid yet.</summary>
    private static PayLaterDebt NewDebt(Payment payment, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PaymentId = payment.Id,
        InvoiceId = payment.InvoiceId,
        Description = payment.Note!.Trim(),
        PayLaterAmount = payment.Amount,
        PaidAmount = 0m,
        IsCompleted = false,
        CreatedUtc = now,
        LastModifiedUtc = now
    };
```

`payment.Note!` is safe here: `SalePaymentRules` has already refused a PayLater without a name.

- [ ] **Step 5: Run the tests to verify they pass**

Run the two commands from Step 3.
Expected: PASS, 5 handler tests and 1 integration test.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleCommandHandlerPayLaterTests.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs
git commit -m "fix(sales): create the debt for a v4 PayLater sale"
```

---

### Task 3: A refused sale is a 400, and a v4 debt works like a migrated one

**Files:**
- Modify: `src/IndyPOS.StoreHub/Program.cs:539-553` (`POST /sales/complete`)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs`

**Interfaces:**
- Consumes: `SaleValidationException` (Task 1); `GET /pay-later` → `GetPayLaterResponse { IReadOnlyList<PayLaterDto> Items }`; `POST /pay-later/{id}/record-payment` with `RecordPaymentRequest(decimal PaymentAmount)` → `PayLaterDto { RemainingAmount }`; `GET /cash/summary` → `CashDrawerSummaryDto { PayLaterGeneralProductsTotal, PayLaterHardwareProductsTotal }`.
- Produces: `400 { error }` on `/sales/complete` for any `SaleValidationException`.

- [ ] **Step 1: Write the failing tests**

Add to `PayLaterSaleEndpointTests.cs`, **above** the regression test, so the negative cases come first:

```csharp
    private sealed record ErrorBody(string Error);

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales/complete", await CreditSaleAsync(note: null));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_ReturnsTheThaiReason()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales/complete", await CreditSaleAsync(note: null));

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Be("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_SavesNoInvoice()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(note: null);
        var before = await CountInvoicesAsync();

        await Client.PostAsJsonAsync("/sales/complete", request);

        (await CountInvoicesAsync()).Should()
                                    .Be(before);
    }

    [Fact]
    public async Task CompleteSale_WithPayLaterMixedWithCash_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(CustomerName, new SalePaymentRequest(PaymentMethodCodes.Cash, 1m));

        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    // Was an unmapped InvalidOperationException: a 500.
    [Fact]
    public async Task CompleteSale_WithAMethodNotOfferable_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(CustomerName) with { Payments = [new SalePaymentRequest(PaymentMethodCodes.M33WeLove, Price)] };

        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }
```

Then add these after the regression test:

```csharp
    [Fact]
    public async Task GetPayLater_AfterAV4CreditSale_ListsItsDebt()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOnCreditAsync();

        var list = await Client.GetFromJsonAsync<GetPayLaterResponse>("/pay-later", JsonOptions);

        list!.Items.Should()
                   .ContainSingle(d => d.InvoiceId == sale.InvoiceId && d.Description == CustomerName);
    }

    [Fact]
    public async Task RecordPayment_OnAV4CreditSale_ReducesWhatIsOwed()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOnCreditAsync();
        var debt = (await Client.GetFromJsonAsync<GetPayLaterResponse>("/pay-later", JsonOptions))!
                   .Items.Single(d => d.InvoiceId == sale.InvoiceId);

        var response = await Client.PostAsJsonAsync($"/pay-later/{debt.Id}/record-payment", new RecordPaymentRequest(100m));

        (await response.Content.ReadFromJsonAsync<PayLaterDto>(JsonOptions))!.RemainingAmount.Should()
                                                                             .Be(Price - 100m);
    }

    // The drawer read credit sales through pay_later rows, so a v4 one counted as cash (spec §1).
    [Fact]
    public async Task CashSummary_AfterAV4CreditSale_CountsItAsCredit()
    {
        await AuthenticateAsCashierAsync();
        var before = await TodaysCreditTotalAsync();

        await SellOnCreditAsync();

        (await TodaysCreditTotalAsync()).Should()
                                        .Be(before + Price);
    }

    private async Task<decimal> TodaysCreditTotalAsync()
    {
        var summary = await Client.GetFromJsonAsync<CashDrawerSummaryDto>("/cash/summary", JsonOptions);

        return summary!.PayLaterGeneralProductsTotal + summary.PayLaterHardwareProductsTotal;
    }

    private async Task<int> CountInvoicesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.Invoices.CountAsync();
    }
```

Add these `using`s: `IndyPOS.Application.UseCases.StoreHub.PayLater;` and `IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PayLaterSaleEndpointTests"`
Expected:
- `…WithoutANote_ReturnsBadRequest`, `…WithoutANote_ReturnsTheThaiReason`, `…MixedWithCash…` and `…NotOfferable…` FAIL, with `500 InternalServerError`;
- `…SavesNoInvoice` PASSES already, because the rules throw before any save (Task 1). It pins that guarantee;
- the regression test and the three positive tests PASS, because Task 2 created the debt.

The RED for the positive tests was the regression test in Task 2. To see these three fail too, run them once with Task 2's `foreach` commented out, then restore it.

- [ ] **Step 3: Map the exception**

In `Program.cs`, replace the body of `POST /sales/complete`:

```csharp
    var response = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(response);
```

with:

```csharp
    // A refused sale is the caller's mistake, not the server's: a Thai reason for the cashier.
    try
    {
        return Results.Ok(await handler.HandleAsync(command, cancellationToken));
    }
    catch (SaleValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
```

Add `using IndyPOS.Application.Common.Exceptions;` if `Program.cs` does not already have it.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PayLaterSaleEndpointTests|FullyQualifiedName~SalesEndpointTests"`
Expected: PASS, 9 PayLater tests. The existing `SalesEndpointTests` still pass: none of them sends PayLater.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/PayLaterSaleEndpointTests.cs
git commit -m "fix(sales): answer a refused sale with a Thai 400, not a 500"
```

---

### Task 4: The till refuses ลงบัญชี once the sale has a payment

**Files:**
- Modify: `src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs:284-287` (the top of `ChangePaymentType`)
- Modify: `CLAUDE.md`, `ONBOARDING.md` (suite counts)

**Interfaces:**
- Consumes: `ISaleService.Payments : IList<Payment>`; `_messageForm.ShowDialog(string message, string title)` (as at `:235`); `SalePaymentRules.IsPayLater(string)` (Task 1).
- Produces: nothing.

**Why no unit test (spec §6 allows this):** the change is one guard in a WinForms event handler, which cannot run without a shown form. Pulling it into a helper for a single `Count > 0` would add structure for no testable logic. The Avalonia port moves this form to a ViewModel, where it will be tested. The server's 400 (Task 3) is the real guard, and it is covered.

- [ ] **Step 1: Add the guard**

In `AcceptPaymentForm.ChangePaymentType`, insert this as the **first** statement, above `_selectedMethodCode = methodCode;` (`:286`), so a refused PayLater never becomes the selected method:

```csharp
			// The store's rule: a credit sale is paid wholly on credit. The v3 till allowed the mix by
			// taking whatever balance remained, and cashiers slipped about 7 times a year (spec §3).
			if (SalePaymentRules.IsPayLater(methodCode) && _saleService.Payments.Count > 0)
			{
				_messageForm.ShowDialog("การลงบัญชีต้องไม่รวมกับการชำระแบบอื่น", "ลงบัญชีไม่ได้");
				return;
			}

```

Add `using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;`, and match the file's tab indentation. Using `IsPayLater` keeps one definition of "is this PayLater", the same one the server uses.

- [ ] **Step 2: Build**

Run: `dotnet build src/IndyPOS.Windows.Forms`
Expected: `Build succeeded.`

- [ ] **Step 3: Manual smoke check**

Use the dev StoreHub, or the VM hot-swap loop. Then:
1. Ring up one item and add a partial cash payment.
2. Open the payment form again and press **ลงบัญชี**.
3. Expected: the Thai message appears, and PayLater is not selected.
4. On a fresh sale with no payment, **ลงบัญชี** still works and takes the whole balance.

Record the result in the PR description.

- [ ] **Step 4: Run the whole suite and measure**

Run: `dotnet test --logger trx --results-directory <dir>`, with Docker running and the real store databases present.
Expected: all green. Application gains 21 (16 rules + 5 handler), and StoreHub.IntegrationTests gains 9. Use the **measured** numbers in the next step, not these.

- [ ] **Step 5: Update the documented counts**

In `CLAUDE.md` and `ONBOARDING.md`, update the total, the pass count, `Application`, `StoreHub.IntegrationTests`, the without-store-databases figure (the total − 20, derived), and the Docker-down figure. StoreHub.IntegrationTests' 9 new tests all need a container, so the Docker-down total grows by 9. Leave no old figure behind: `grep -n` each old number in both files.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs CLAUDE.md ONBOARDING.md
git commit -m "fix(till): refuse PayLater once the sale already has a payment"
```

---

## Self-Review

**Spec coverage** (spec § → task):
- §4.1 one debt per PayLater payment, same save, `Description` trimmed, `PaidAmount` 0, ignoring case → Task 2.
- §4.2 all seven rules, before any save → Task 1.
- §4.3 400 `{ error }` on `/sales/complete`; a refused sale writes nothing → Task 3 (+ Task 1's `SavesNothing`).
- §5 till button rule → Task 4. Cash summary now counts v4 credit → Task 3 (`CashSummary_AfterAV4CreditSale_CountsItAsCredit`).
- §6 handler tests → Tasks 1, 2. Integration tests (regression, list, repay, 400s, no invoice saved, summary) → Tasks 2, 3. Till → Task 4 (manual, as §6 allows).

**Deviations from the spec, called out:**
- **D1: no new `ISaleRepository.CompleteSaleAsync` parameter.** The debt rides `Payment.PayLater`, the existing 1:1 navigation, into the same `SaveChangesAsync`. It is atomic, as §4.1 requires, and it keeps the signature that 7 Moq setups and invoice-history plan 1 depend on.
- **D2: the rules live in `SalePaymentRules`,** a pure static class, rather than inline in the handler. The handler stays short, and the rules get plain unit tests.
- **D3: the till refuses at the method button, with a message,** rather than hiding ลงบัญชี. Hiding it would leave the cashier with no button to press and no reason why.

**Placeholder scan:** none left. `<dir>` is any results folder. The Task 4 counts are derived, and Step 5 uses the measured ones.

**Type consistency:**
- `SaleValidationException(string)` (Tasks 1, 3).
- `SalePaymentRules.EnsureValid(IReadOnlyList<SalePaymentRequest>, decimal, IReadOnlySet<string>)`, `IsPayLater(string)` and `MaxCustomerNameLength` (Tasks 1, 2).
- `PayLaterDebt` alias (Task 2).
- `CreditSaleAsync(string? note, params SalePaymentRequest[])`, `SellOnCreditAsync(string?)`, `CountDebtsOfAsync(Guid)` and `CountInvoicesAsync()` (Tasks 2, 3).

**Review Focus check:** all five have tests, as named above.

**Outside this plan's scope (flag, don't fix):**
- The till shows a generic "StoreHub request failed: 400" for these refusals (`StoreHubHttpClient.EnsureSuccessfulResponseAsync`). Showing the server's Thai `error` is cash plan 3's work.
- `UserId` still comes from the request body on `/sales/complete` (route tidy-up PR).
