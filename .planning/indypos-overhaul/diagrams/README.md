# IndyPOS Architecture Diagrams

Version: 2.0.0
Date: 2026-03-31
Status: ✅ Updated for Epic H completion (93% overall progress)

This is the **main diagrams folder** for the IndyPOS overhaul project.

This folder contains comprehensive ASCII diagrams documenting the IndyPOS offline-first architecture overhaul.

## Diagram Index

### 01. Architecture Overview
**File:** `01-architecture-overview.md`

**Contents:**
- Current vs Target architecture side-by-side
- Authentication & authorization flow
- API endpoints summary
- Deployment models (Store + Cloud)
- .NET Aspire development environment

**Use this when:** Understanding the big picture transformation

---

### 02. Data Flow
**File:** `02-data-flow.md`

**Contents:**
- Complete sale flow (happy path)
- Background sync (Outbox → Cloud)
- Retry flow (internet down)
- Master data pull (Cloud → Store)
- Inventory adjustment flow
- Stock query flow

**Use this when:** Understanding how data moves through the system

---

### 03. Component Relationships
**File:** `03-component-relationships.md`

**Contents:**
- Clean Architecture layers
- Project structure mapping
- Dependency flow diagram
- Communication patterns
- Cross-cutting concerns

**Use this when:** Understanding how code is organized

---

### 04. Database Schema
**File:** `04-database-schema.md`

**Contents:**
- StoreHub database tables (local PostgreSQL)
- Cloud database tables (central PostgreSQL)
- User & Auth tables (Epic S)
- OAuth2 / OpenIddict tables
- Entity relationships
- InvoiceCompletedEvent payload

**Use this when:** Designing schema or writing migrations

---

### 05. Sync Flow (Outbox Pattern)
**File:** `05-sync-flow-outbox-pattern.md`

**Contents:**
- Outbox pattern detailed explanation
- Complete sale with outbox (phase by phase)
- Idempotency guarantee
- Failure scenarios & recovery
- Monitoring & observability

**Use this when:** Implementing sync logic or debugging sync issues

---

### 06. Epic Roadmap
**File:** `06-epic-roadmap.md`

**Contents:**
- Epic dependency graph (with completion status)
- Sprint breakdown (all 6 sprints)
- Current status (93% complete)
- Remaining work (Epic S6-S9, Epic I)
- Success criteria by epic

**Use this when:** Planning sprints or understanding dependencies

---

### 07. Business Logic Layers
**File:** `07-business-logic-layers.md`

**Contents:**
- Current state diagram (UI-heavy architecture)
- Target state diagram (proper layering)
- Business logic migration map
- Duplicated logic to consolidate
- Extraction priority

**Use this when:** Understanding where business logic lives and where it should go

---

### 08. Extraction Plan
**File:** `08-extraction-plan.md`

**Contents:**
- Step-by-step extraction tasks per module
- Sales, Payment, PayLater, CashFlow modules
- New files to create
- PR plan (~11 small PRs)
- Testing strategy
- Success criteria

**Use this when:** Implementing Epic 0 (Extract Business Logic)

---

### 09. Terminal Concurrency
**File:** `09-terminal-concurrency.md`

**Contents:**
- Multi-terminal architecture (2 terminals → 1 StoreHub)
- PostgreSQL transaction locking
- Invoice number generation strategy
- Inventory movement pattern
- Why NOT separate databases

**Use this when:** Understanding multi-terminal safety for Epic C

---

### 10. .NET Aspire Development Environment
**File:** `10-aspire-dev-environment.md`

**Contents:**
- Aspire orchestration architecture
- AppHost configuration example
- ServiceDefaults project
- Developer workflow
- Development vs Production comparison

**Use this when:** Setting up or understanding the dev environment (Epic C)

---

### 11. Solution Structure
**File:** `11-solution-structure.md`

**Contents:**
- Solution folder structure (15 projects)
- Project dependency graph
- Desktop client architecture (Epic G)
- Test project architecture (266+ tests)
- Dependency rules
- File system layout
- Package dependencies

**Use this when:** Understanding project organization and dependencies

---

### 12. Security & Authentication Flow ✨ NEW
**File:** `12-security-auth-flow.md`

**Contents:**
- Authentication architecture (Store vs Cloud)
- StoreHub login flow (offline-capable)
- CloudApi OAuth2 flow (OpenIddict)
- Store registration flow
- RBAC capability matrix
- Authorization handler flow
- Secret storage (RSA + DPAPI)
- JWT token structure
- Implementation status

**Use this when:** Understanding authentication, authorization, or security features

---

## How to Use These Diagrams

### For Planning
1. Start with `06-epic-roadmap.md` to understand the implementation order
2. Review `01-architecture-overview.md` for the target state
3. Check specific diagrams for epic you're working on

### For Implementation
1. Review `03-component-relationships.md` to understand where code goes
2. Check `04-database-schema.md` for schema details
3. Reference `02-data-flow.md` for specific flows you're implementing
4. Check `12-security-auth-flow.md` for auth requirements

### For Debugging
1. Trace through `02-data-flow.md` to find where things break
2. Review `05-sync-flow-outbox-pattern.md` for sync issues
3. Check `04-database-schema.md` for query problems
4. Review `12-security-auth-flow.md` for auth issues

### For Onboarding New Developers
Read in this order:
1. `01-architecture-overview.md` (big picture)
2. `11-solution-structure.md` (project organization)
3. `03-component-relationships.md` (code organization)
4. `06-epic-roadmap.md` (implementation plan)
5. `12-security-auth-flow.md` (security model)
6. Others as needed

---

## Maintenance

When updating diagrams:
1. Increment version number in the diagram file
2. Update "Last Updated" date
3. Update this README if new diagrams are added
4. Keep diagrams in sync with actual implementation

---

## Diagram Conventions

### Box Styles
```
┌─────────┐
│ System  │  Standard component
└─────────┘

╔═════════╗
║ Process ║  Critical/emphasized component (or completed epic)
╚═════════╝

[Optional]  Optional or future component
```

### Arrows
```
───►  Required dependency / data flow
- - ►  Optional dependency
───┐
   └─►  Branch / multiple destinations
```

### Status Indicators
```
✅  Completed / Working
🔴  Not started / Blocked
🟡  In progress
⚠️   Warning / Attention needed
❌  Deprecated / Removed
⏭️   Skipped / Deferred
```

---

## Quick Stats

| Metric | Value |
|--------|-------|
| Total Epics | 11 (including Epic 0) |
| Completed Epics | 8 |
| In Progress | 2 (S, I) |
| Tests Passing | 266+ |
| Build Status | 0 Warnings, 0 Errors |
| Progress | 93% |

---

**Related Documentation:**
- Implementation Status: `../implementation-status.md`
- Project Context: `../../../CLAUDE.md`
- v1.4.0 Docs: `../IndyPOS_Docs_v1_4_0/`
- Security Spec: `../security/indypos_security_design_spec.md`
- Operations: `../../../docs/operations/RUNBOOK.md`
