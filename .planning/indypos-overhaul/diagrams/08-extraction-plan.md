# Business Logic Extraction Plan

Version: 1.0.0
Date: 2026-03-05

This document details the step-by-step plan to extract business logic from UI codebehind to proper Application/Domain layers.

---

## Overview

### Goal
Extract business logic from Windows.Forms codebehind files to Application/Domain layers, ensuring:
1. All existing functionality is preserved
2. Code becomes testable without UI
3. Logic is centralized (no duplication)
4. Ready for StoreHub API migration

### Approach
- Small, incremental PRs
- Each module extracted independently
- Tests written before/during extraction
- UI codebehind becomes thin (calls services)

---

## Epic 0: Extract Business Logic

### 0.1 Sales Module Extraction

**Source Files:**
- `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs`
- `src/IndyPOS.Windows.Forms/UI/Sale/AddInvoiceProductForm.cs`
- `src/IndyPOS.Windows.Forms/UI/Sale/UpdateInvoiceProductForm.cs`

**Logic to Extract:**

| Method | Current Location | Target Location |
|--------|-----------------|-----------------|
| `CalculateInvoiceTotal()` | SalePanel.cs:168 | `ISaleCalculationService.CalculateTotal()` |
| `CalculateChanges()` | SalePanel.cs:169 | `ISaleCalculationService.CalculateChange()` |
| Product total formula | SalePanel.cs:185 | `InvoiceLine.GetTotal()` (Domain) |
| `ValidateSaleInvoice()` | SalePanel.cs:238 | `ISaleValidator.Validate()` |
| `CompleteSaleAsync()` | SalePanel.cs:256 | `ICompleteSaleUseCase.ExecuteAsync()` |
| `IsPendingPayment()` | SalePanel.cs:221 | `Invoice.HasPendingPayment()` (Domain) |
| Quantity validation | AddInvoiceProductForm.cs:48 | `QuantityValidator` |
| Price validation | AddInvoiceProductForm.cs:65 | `PriceValidator` |

**New Files to Create:**
```
src/IndyPOS.Application/
├── Sales/
│   ├── Services/
│   │   ├── ISaleCalculationService.cs
│   │   └── SaleCalculationService.cs
│   ├── Validators/
│   │   ├── ISaleValidator.cs
│   │   └── SaleValidator.cs
│   └── UseCases/
│       ├── ICompleteSaleUseCase.cs
│       └── CompleteSaleUseCase.cs

src/IndyPOS.Domain/
├── Sales/
│   └── Extensions/
│       └── InvoiceExtensions.cs  (or rich entity methods)
```

**Tasks:**
- [ ] 0.1.1: Create `ISaleCalculationService` interface
- [ ] 0.1.2: Implement `SaleCalculationService` (move logic from SalePanel)
- [ ] 0.1.3: Create `ISaleValidator` interface
- [ ] 0.1.4: Implement `SaleValidator` (move validation logic)
- [ ] 0.1.5: Update `SalePanel` to use services
- [ ] 0.1.6: Add unit tests for calculation service
- [ ] 0.1.7: Add unit tests for validator

**PR Size:** ~3 small PRs

---

### 0.2 Payment Module Extraction

**Source Files:**
- `src/IndyPOS.Windows.Forms/UI/Payment/AcceptPaymentForm.cs`

**Logic to Extract:**

| Method | Current Location | Target Location |
|--------|-----------------|-----------------|
| `CalculateBalanceRemaining()` | AcceptPaymentForm.cs:51 | `IPaymentCalculationService.CalculateBalance()` |
| `IsRefundInvoice()` | AcceptPaymentForm.cs:55 | `Invoice.IsRefund` property (Domain) |
| Refund method rules | AcceptPaymentForm.cs:69-114 | `IPaymentRulesService.GetAllowedMethods()` |
| PayLater completion | AcceptPaymentForm.cs:135-142 | `PayLater.IsCompleted` (Domain) |
| Money accumulation | AcceptPaymentForm.cs:218-226 | `Money` Value Object |
| Payment processing | AcceptPaymentForm.cs:328-345 | `IProcessPaymentUseCase` |

**New Files to Create:**
```
src/IndyPOS.Application/
├── Payments/
│   ├── Services/
│   │   ├── IPaymentCalculationService.cs
│   │   ├── PaymentCalculationService.cs
│   │   ├── IPaymentRulesService.cs
│   │   └── PaymentRulesService.cs
│   └── UseCases/
│       ├── IProcessPaymentUseCase.cs
│       └── ProcessPaymentUseCase.cs

src/IndyPOS.Domain/
├── Payments/
│   ├── ValueObjects/
│   │   └── Money.cs
│   └── Enums/
│       └── PaymentMethod.cs (if not exists)
```

**Tasks:**
- [ ] 0.2.1: Create `Money` Value Object (Domain)
- [ ] 0.2.2: Create `IPaymentCalculationService` interface
- [ ] 0.2.3: Implement `PaymentCalculationService`
- [ ] 0.2.4: Create `IPaymentRulesService` for refund/method rules
- [ ] 0.2.5: Implement `PaymentRulesService`
- [ ] 0.2.6: Update `AcceptPaymentForm` to use services
- [ ] 0.2.7: Add unit tests

**PR Size:** ~3 small PRs

---

### 0.3 PayLater Module Extraction

**Source Files:**
- `src/IndyPOS.Windows.Forms/UI/PayLater/PayLaterPaymentPanel.cs`
- `src/IndyPOS.Windows.Forms/UI/Report/PayLaterPaymentsReportPanel.cs`

**Logic to Extract:**

| Method | Current Location | Target Location |
|--------|-----------------|-----------------|
| Completion calculation | PayLaterPaymentPanel.cs:126-143 | `PayLater.IsCompleted` (Domain) |
| Filter incomplete | PayLaterPaymentPanel.cs:145-160 | `IPayLaterQueryService.GetPending()` |
| Payment grouping | PayLaterPaymentsReportPanel.cs:86-97 | `IPayLaterReportService.GetSummary()` |
| Balance calculation | PayLaterPaymentsReportPanel.cs:99-116 | `PayLater.RemainingAmount` (Domain) |

**New Files to Create:**
```
src/IndyPOS.Application/
├── PayLater/
│   ├── Services/
│   │   ├── IPayLaterService.cs
│   │   └── PayLaterService.cs
│   └── Queries/
│       ├── IPayLaterQueryService.cs
│       └── PayLaterQueryService.cs

src/IndyPOS.Domain/
├── PayLater/
│   └── Entities/
│       └── PayLater.cs  (enrich existing or create)
```

**Domain Entity Enhancement:**
```csharp
// src/IndyPOS.Domain/PayLater/Entities/PayLater.cs
public class PayLater
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; }  // Customer name
    public decimal PayLaterAmount { get; private set; }
    public decimal PaidAmount { get; private set; }

    // Business logic in entity
    public decimal RemainingAmount => PayLaterAmount - PaidAmount;
    public bool IsCompleted => PaidAmount >= PayLaterAmount;

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive");
        if (amount > RemainingAmount) throw new InvalidOperationException("Amount exceeds remaining");
        PaidAmount += amount;
    }
}
```

**Tasks:**
- [ ] 0.3.1: Enrich `PayLater` entity with business logic
- [ ] 0.3.2: Create `IPayLaterService` interface
- [ ] 0.3.3: Implement `PayLaterService`
- [ ] 0.3.4: Update `PayLaterPaymentPanel` to use service
- [ ] 0.3.5: Update `PayLaterPaymentsReportPanel` to use service
- [ ] 0.3.6: Add unit tests for PayLater entity
- [ ] 0.3.7: Add unit tests for PayLater service

**PR Size:** ~2 small PRs

---

### 0.4 CashFlow Module Extraction

**Source Files:**
- `src/IndyPOS.Windows.Forms/UI/Report/CashFlowCalculatorPanel.cs`

**Logic to Extract:**

| Method | Current Location | Target Location |
|--------|-----------------|-----------------|
| `CalculateExpectedCash()` | CashFlowCalculatorPanel.cs:326-336 | `ICashFlowCalculator.CalculateExpected()` |
| `CalculateActualCash()` | CashFlowCalculatorPanel.cs:338-351 | `ICashFlowCalculator.CalculateActual()` |
| Cash difference | CashFlowCalculatorPanel.cs:282-324 | `ICashFlowCalculator.GetDifference()` |
| Currency denomination count | CashFlowCalculatorPanel.cs:574-644 | `CurrencyCount` Value Object |
| Report generation | CashFlowCalculatorPanel.cs:52-72 | `ICashFlowReportService` |
| File persistence | CashFlowCalculatorPanel.cs:154-201 | `ICashFlowExportService` (Infrastructure) |

**New Files to Create:**
```
src/IndyPOS.Application/
├── CashFlow/
│   ├── Services/
│   │   ├── ICashFlowCalculator.cs
│   │   ├── CashFlowCalculator.cs
│   │   ├── ICashFlowReportService.cs
│   │   └── CashFlowReportService.cs
│   └── Models/
│       ├── CashFlowData.cs
│       └── CurrencyCount.cs

src/IndyPOS.Infrastructure/
├── CashFlow/
│   ├── ICashFlowExportService.cs
│   └── CashFlowExportService.cs  (JSON/CSV export)
```

**Cash Flow Formula (to preserve):**
```csharp
// Application/CashFlow/Services/CashFlowCalculator.cs
public decimal CalculateExpectedCash(CashFlowData data)
{
    return data.SalesTotalWithoutPayLater
         + data.ReceivedPayLaterPayments
         + data.ChangesTotal
         - data.MoneyTransferTotal
         - data.WelfareCardTotal
         - data.PayoutsTotal;
}

public decimal CalculateActualCash(CurrencyCount count)
{
    return count.BankNote1000 * 1000
         + count.BankNote500 * 500
         + count.BankNote100 * 100
         + count.BankNote50 * 50
         + count.BankNote20 * 20
         + count.Coin10 * 10
         + count.Coin5 * 5
         + count.Coin2 * 2
         + count.Coin1 * 1;
}
```

**Tasks:**
- [ ] 0.4.1: Create `CashFlowData` and `CurrencyCount` models
- [ ] 0.4.2: Create `ICashFlowCalculator` interface
- [ ] 0.4.3: Implement `CashFlowCalculator` (move formulas)
- [ ] 0.4.4: Create `ICashFlowExportService` (Infrastructure)
- [ ] 0.4.5: Update `CashFlowCalculatorPanel` to use services
- [ ] 0.4.6: Add unit tests for calculator
- [ ] 0.4.7: Remove hardcoded paths (use configuration)

**PR Size:** ~3 small PRs

---

### 0.5 Product Calculation Consolidation

**Source Files (duplicated logic):**
- `src/IndyPOS.Windows.Forms/UI/Sale/SalePanel.cs:185`
- `src/IndyPOS.Windows.Forms/UI/Report/SalesHistoryReportPanel.cs:237`
- `src/IndyPOS.Windows.Forms/UI/Report/InvoiceProductsReportPanel.cs:92`

**Single Source of Truth:**
```csharp
// src/IndyPOS.Domain/Sales/Entities/InvoiceLine.cs
public class InvoiceLine
{
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public bool IsGroupProduct { get; private set; }
    public decimal GroupPrice { get; private set; }

    public decimal GetTotal() => IsGroupProduct ? GroupPrice : UnitPrice * Quantity;
}
```

**Tasks:**
- [ ] 0.5.1: Add `GetTotal()` method to `InvoiceLine` entity (or DTO)
- [ ] 0.5.2: Update `SalePanel` to use entity method
- [ ] 0.5.3: Update `SalesHistoryReportPanel` to use entity method
- [ ] 0.5.4: Update `InvoiceProductsReportPanel` to use entity method
- [ ] 0.5.5: Add unit test for `GetTotal()` formula

**PR Size:** 1 small PR

---

## Dependency Graph

```
0.5 Product Calculation ─────┐
                             │
0.1 Sales Module ◄───────────┤
        │                    │
        ▼                    │
0.2 Payment Module ◄─────────┤
        │                    │
        ▼                    │
0.3 PayLater Module ◄────────┘
        │
        ▼
0.4 CashFlow Module (independent, can parallel)
```

**Recommended Order:**
1. **0.5** Product Calculation (foundation)
2. **0.1** Sales Module (core)
3. **0.2** Payment Module (depends on Sales)
4. **0.3** PayLater Module (depends on Payment)
5. **0.4** CashFlow Module (can be parallel with 0.2-0.3)

---

## Testing Strategy

### Unit Tests Required

| Module | Test File | Coverage |
|--------|-----------|----------|
| Sales | `SaleCalculationServiceTests.cs` | Total calc, change calc |
| Sales | `SaleValidatorTests.cs` | All validation scenarios |
| Payment | `PaymentCalculationServiceTests.cs` | Balance calc, refund rules |
| Payment | `MoneyValueObjectTests.cs` | Add, subtract, validation |
| PayLater | `PayLaterEntityTests.cs` | Completion, remaining amount |
| CashFlow | `CashFlowCalculatorTests.cs` | Expected, actual, difference |
| Product | `InvoiceLineTests.cs` | GetTotal formula |

### Integration Tests
- End-to-end sale completion
- PayLater payment recording
- CashFlow report generation

---

## PR Plan

| PR # | Title | Scope | Est. Files |
|------|-------|-------|-----------|
| PR-1 | Extract Product calculation to Domain | 0.5 | ~5 files |
| PR-2 | Create Sale calculation service | 0.1.1-0.1.2 | ~4 files |
| PR-3 | Create Sale validator service | 0.1.3-0.1.5 | ~4 files |
| PR-4 | Add Sale service unit tests | 0.1.6-0.1.7 | ~2 files |
| PR-5 | Create Money value object | 0.2.1 | ~2 files |
| PR-6 | Create Payment calculation service | 0.2.2-0.2.5 | ~5 files |
| PR-7 | Update AcceptPaymentForm to use services | 0.2.6-0.2.7 | ~3 files |
| PR-8 | Enrich PayLater entity | 0.3.1 | ~2 files |
| PR-9 | Create PayLater service | 0.3.2-0.3.5 | ~4 files |
| PR-10 | Create CashFlow calculator | 0.4.1-0.4.3 | ~5 files |
| PR-11 | Create CashFlow export service | 0.4.4-0.4.7 | ~4 files |

**Total: ~11 small PRs**

---

## Success Criteria

### Per Module
- [ ] All business logic moved to Application/Domain
- [ ] UI codebehind only calls services
- [ ] Unit tests pass with >80% coverage on business logic
- [ ] No duplicate logic remaining

### Overall
- [ ] Build passes
- [ ] All existing functionality works
- [ ] No regressions in UI behavior
- [ ] Ready for StoreHub API reuse

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking existing UI | Small PRs, test after each |
| Missing edge cases | Extract tests from UI event handlers |
| Logic differences | Compare before/after behavior |
| Regression | Manual testing checklist per module |

---

## Next Steps

After Epic 0 is complete:
1. **Epic B**: Remove deprecated PG report (safe now)
2. **Epic A**: Add StoreId concept
3. **Epic D**: Schema design (reuse extracted services)
4. **Epic C**: StoreHub API (reuse Application layer)

---

**Related:**
- `07-business-logic-layers.md` - Current vs target diagrams
- `implementation-status.md` - Overall progress tracking
