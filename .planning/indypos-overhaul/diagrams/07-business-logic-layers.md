# Business Logic Layers - Current vs Target

Version: 1.1.0
Date: 2026-03-06

**Status:** UPDATED after Epic 0 implementation

This diagram documents where business logic currently lives vs where it should live after the extraction.

---

## Current State: UI-Heavy Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          WINDOWS.FORMS (Presentation)                            │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                        BUSINESS LOGIC IN UI (!)                            │ │
│  │                                                                             │ │
│  │  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐       │ │
│  │  │   SalePanel.cs    │  │AcceptPaymentForm  │  │PayLaterPayment    │       │ │
│  │  │                   │  │      .cs          │  │    Panel.cs       │       │ │
│  │  │ • CalculateTotal  │  │                   │  │                   │       │ │
│  │  │ • CalculateChange │  │ • BalanceCalc     │  │ • IsCompleted     │       │ │
│  │  │ • ValidateSale    │  │ • RefundLogic     │  │ • PaymentFilter   │       │ │
│  │  │ • CompleteSale    │  │ • PaymentProcess  │  │ • UpdatePayment   │       │ │
│  │  └───────────────────┘  └───────────────────┘  └───────────────────┘       │ │
│  │                                                                             │ │
│  │  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐       │ │
│  │  │CashFlowCalculator │  │InvoiceProductsRpt │  │  UsersPanel.cs    │       │ │
│  │  │    Panel.cs       │  │    Panel.cs       │  │                   │       │ │
│  │  │                   │  │                   │  │ • RoleBasedAccess │       │ │
│  │  │ • ExpectedCash    │  │ • ProductTotal    │  │ • UserVisibility  │       │ │
│  │  │ • ActualCash      │  │ • Categorization  │  │ • PasswordDecrypt │       │ │
│  │  │ • CashDifference  │  │   (DUPLICATE)     │  │                   │       │ │
│  │  │ • FileOperations  │  │                   │  │                   │       │ │
│  │  └───────────────────┘  └───────────────────┘  └───────────────────┘       │ │
│  │                                                                             │ │
│  │  ⚠️  ISSUES:                                                                │ │
│  │  • Product total calc duplicated in 3 files                                │ │
│  │  • Validation scattered across forms                                       │ │
│  │  • Business rules mixed with UI events                                     │ │
│  │  • Hard to test without launching UI                                       │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                  │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
                                   ▼ (Thin - mostly data access)
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                      │
│                                                                                  │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐      │
│  │  ISaleService       │  │  IPaymentService    │  │  IReportService     │      │
│  │  (Interface only)   │  │  (Interface only)   │  │  (Query only)       │      │
│  │                     │  │                     │  │                     │      │
│  │  No business logic  │  │  No business logic  │  │  GetSalesReport()   │      │
│  │  Just data pass-thru│  │  Just data pass-thru│  │  GetPaymentsReport()│      │
│  └─────────────────────┘  └─────────────────────┘  └─────────────────────┘      │
│                                                                                  │
│  ┌─────────────────────┐  ┌─────────────────────┐                               │
│  │  IPayLaterService   │  │  IUserService       │                               │
│  │  (Query only)       │  │  (CRUD only)        │                               │
│  └─────────────────────┘  └─────────────────────┘                               │
│                                                                                  │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                        │
│                                                                                  │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐      │
│  │  Invoice (Entity)   │  │  Payment (Entity)   │  │  Product (Entity)   │      │
│  │                     │  │                     │  │                     │      │
│  │  Anemic - no logic  │  │  Anemic - no logic  │  │  Anemic - no logic  │      │
│  └─────────────────────┘  └─────────────────────┘  └─────────────────────┘      │
│                                                                                  │
│  ⚠️  Domain entities are "anemic" - just data containers                         │
│  ⚠️  No domain services, no value objects, no business rules                     │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Target State: Proper Layering

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          WINDOWS.FORMS (Presentation)                            │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                        UI ONLY - No Business Logic                          │ │
│  │                                                                             │ │
│  │  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐       │ │
│  │  │   SalePanel.cs    │  │AcceptPaymentForm  │  │PayLaterPayment    │       │ │
│  │  │                   │  │      .cs          │  │    Panel.cs       │       │ │
│  │  │ • Bind data       │  │                   │  │                   │       │ │
│  │  │ • Call service    │  │ • Show dialog     │  │ • Display list    │       │ │
│  │  │ • Show results    │  │ • Capture input   │  │ • Handle clicks   │       │ │
│  │  │ • Handle events   │  │ • Call service    │  │ • Call service    │       │ │
│  │  └───────────────────┘  └───────────────────┘  └───────────────────┘       │ │
│  │                                                                             │ │
│  │  ✅ BENEFITS:                                                               │ │
│  │  • UI is testable (mock services)                                          │ │
│  │  • Logic is in one place                                                   │ │
│  │  • Easy to port to MAUI later                                              │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                  │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
                                   ▼ (Rich - all orchestration here)
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                      │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          SALE MODULE                                       │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │ ISaleService        │  │ SaleService         │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ CalculateTotal()    │◄─┤ Uses Domain logic   │                         │  │
│  │  │ CalculateChange()   │  │ Orchestrates flow   │                         │  │
│  │  │ ValidateSale()      │  │ Transaction mgmt    │                         │  │
│  │  │ CompleteSaleAsync() │  │                     │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐                                                  │  │
│  │  │ ISaleValidator      │  Validates invoice before completion             │  │
│  │  └─────────────────────┘                                                  │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          PAYMENT MODULE                                    │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │ IPaymentService     │  │ PaymentService      │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ CalculateBalance()  │◄─┤ Uses Money VO       │                         │  │
│  │  │ ProcessPayment()    │  │ Payment validation  │                         │  │
│  │  │ ProcessRefund()     │  │ Refund rules        │                         │  │
│  │  │ GetAllowedMethods() │  │                     │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          PAYLATER MODULE                                   │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │ IPayLaterService    │  │ PayLaterService     │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ IsCompleted()       │◄─┤ Uses PayLater VO    │                         │  │
│  │  │ RecordPayment()     │  │ Completion rules    │                         │  │
│  │  │ GetPending()        │  │ Balance calc        │                         │  │
│  │  │ GetByCustomer()     │  │                     │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          CASHFLOW MODULE                                   │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │ ICashFlowService    │  │ CashFlowService     │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ CalculateExpected() │◄─┤ Uses Money VO       │                         │  │
│  │  │ CalculateActual()   │  │ All formulas here   │                         │  │
│  │  │ GetDifference()     │  │ Currency counting   │                         │  │
│  │  │ GenerateReport()    │  │                     │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
└──────────────────────────────────┬──────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                        │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          VALUE OBJECTS                                     │  │
│  │                                                                            │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐            │  │
│  │  │  Money          │  │  Quantity       │  │  ProductTotal   │            │  │
│  │  │                 │  │                 │  │                 │            │  │
│  │  │ + Amount        │  │ + Value         │  │ + Calculate()   │            │  │
│  │  │ + Currency      │  │ + IsPositive()  │  │   (single place)│            │  │
│  │  │ + Add()         │  │ + Validate()    │  │                 │            │  │
│  │  │ + Subtract()    │  │                 │  │                 │            │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘            │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          ENTITIES (Rich)                                   │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │  Invoice (Entity)   │  │  PayLater (Entity)  │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ + GetTotal()        │  │ + RemainingAmount   │                         │  │
│  │  │ + AddLine()         │  │ + IsCompleted       │                         │  │
│  │  │ + CanComplete()     │  │ + RecordPayment()   │                         │  │
│  │  │ + GetPaymentDue()   │  │ + CanAcceptPayment()│                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │  Payment (Entity)   │  │  Product (Entity)   │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ + IsRefund          │  │ + CalculateTotal()  │                         │  │
│  │  │ + CanProcess()      │  │ + IsGroupProduct    │                         │  │
│  │  │ + Apply()           │  │ + Category          │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                          DOMAIN SERVICES                                   │  │
│  │                                                                            │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │ IProductPricing     │  │ IPaymentRules       │                         │  │
│  │  │                     │  │                     │                         │  │
│  │  │ CalculateLineTotal()│  │ GetAllowedMethods() │                         │  │
│  │  │ ApplyDiscount()     │  │ CanRefund()         │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ✅ BENEFITS:                                                                    │
│  • Business rules in one place                                                  │
│  • Value objects ensure valid state                                             │
│  • Entities encapsulate behavior                                                │
│  • Testable without UI or database                                              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Business Logic Migration Map

| Current Location | Logic | Target Location |
|-----------------|-------|-----------------|
| `SalePanel.cs:168` | `CalculateInvoiceTotal()` | `ISaleService` / `Invoice.GetTotal()` |
| `SalePanel.cs:169` | `CalculateChanges()` | `ISaleService.CalculateChange()` |
| `SalePanel.cs:185` | Group price calculation | `Product.CalculateTotal()` (VO) |
| `SalePanel.cs:238` | `ValidateSaleInvoice()` | `ISaleValidator` |
| `SalePanel.cs:256` | `CompleteSaleAsync()` | `ISaleService.CompleteSaleAsync()` |
| `AcceptPaymentForm.cs:51` | `CalculateBalanceRemaining()` | `IPaymentService.CalculateBalance()` |
| `AcceptPaymentForm.cs:55` | `IsRefundInvoice()` | `Payment.IsRefund` property |
| `AcceptPaymentForm.cs:69-114` | Refund rules | `IPaymentRules.GetAllowedMethods()` |
| `AcceptPaymentForm.cs:135-142` | PayLater completion | `PayLater.IsCompleted` property |
| `PayLaterPaymentPanel.cs:126-143` | Payment completion calc | `PayLater` entity |
| `CashFlowCalculatorPanel.cs:326-351` | Cash reconciliation | `ICashFlowService` |
| `InvoiceProductsReportPanel.cs:110-118` | Product categorization | `Product.Category` / `ProductCategory` enum |
| `UsersPanel.cs:147-162` | Role-based visibility | `IAuthorizationService` |

---

## Duplicated Logic to Consolidate

### Product Total Calculation (3 places → 1)

**Current (scattered):**
```csharp
// SalePanel.cs:185
var total = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;

// SalesHistoryReportPanel.cs:237
var total = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;

// InvoiceProductsReportPanel.cs:92
var total = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;
```

**Target (single source):**
```csharp
// Domain/ValueObjects/ProductTotal.cs
public record ProductTotal
{
    public static decimal Calculate(decimal unitPrice, int quantity, bool isGroupProduct, decimal groupPrice)
        => isGroupProduct ? groupPrice : unitPrice * quantity;
}

// Or as entity method
// Domain/Entities/InvoiceLine.cs
public decimal GetTotal() => IsGroupProduct ? GroupPrice : UnitPrice * Quantity;
```

---

## Extraction Priority

| Priority | Module | Reason |
|----------|--------|--------|
| 1 | **Sales** | Core business flow, most complex |
| 2 | **Payments** | Tightly coupled with Sales |
| 3 | **PayLater** | Critical feature for customers |
| 4 | **CashFlow** | Complex but isolated |
| 5 | **Authorization** | Lower risk, can be done later |

---

---

## Epic 0 Completion Summary (2026-03-06)

### Completed Extractions:

| Item | Before | After | Tests |
|------|--------|-------|-------|
| Product Total | 8 duplicate formulas | `Product.GetTotal()` / `InvoiceProductDto.GetTotal()` | ✅ 4 tests |
| PayLater Completion | UI inline calc | `PayLaterPayment.RecordPayment()` / `PayLaterPaymentDto.WouldBeCompletedWith()` | ✅ 5 tests |
| CashFlow Calc | UI static methods | `CashFlowData.CalculateExpectedCash()` / `CalculateActualCash()` | ✅ 8 tests |

### Files Modified:
- `src/IndyPOS.Application/Common/Models/Product.cs` - Added `GetTotal()`
- `src/IndyPOS.Application/Common/Models/CashFlowData.cs` - Added calculation methods
- `src/IndyPOS.Application/UseCases/InvoiceProducts/InvoiceProductDto.cs` - Added `GetTotal()`
- `src/IndyPOS.Application/UseCases/PayLaterPayments/PayLaterPaymentDto.cs` - Added `WouldBeCompletedWith()`, `RemainingAmount`
- `src/IndyPOS.Domain/Entities/PayLaterPayment.cs` - Added `RecordPayment()`, `RemainingAmount`, `WouldBeCompletedWith()`
- UI files updated to use new methods

### Tests Added:
- `tests/IndyPOS.Application.Tests/Models/ProductTests.cs`
- `tests/IndyPOS.Application.Tests/Models/CashFlowDataTests.cs`
- `tests/IndyPOS.Application.Tests/InvoiceProducts/InvoiceProductDtoTests.cs`
- `tests/IndyPOS.Application.Tests/PayLaterPayments/PayLaterPaymentDtoTests.cs`

### Business Rules Documented:
- PayLater exclusive payment type (cannot mix with other payment methods)
- See `PAYLATER-FEATURE.md` for full documentation

**Next:** Proceed to Epic B (Remove deprecated PG report feature)
