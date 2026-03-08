# IndyPOS Architecture Diagrams

Version: 1.2.0
Date: 2026-03-08

This is the **main diagrams folder** for the IndyPOS overhaul project.

This folder contains comprehensive ASCII diagrams documenting the IndyPOS offline-first architecture overhaul.

## Diagram Index

### 01. Architecture Overview
**File:** `01-architecture-overview.md`

**Contents:**
- Current vs Target architecture side-by-side
- Component breakdown
- Network resilience comparison
- Deployment model

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
- Entity relationships
- Query patterns
- Index strategy

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
- Epic dependency graph
- Sprint breakdown (6 sprints / ~10 weeks)
- Critical path
- Parallel work opportunities
- Risk mitigation
- Success criteria

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

### 09. Terminal Concurrency ✨ NEW
**File:** `09-terminal-concurrency.md`

**Contents:**
- Multi-terminal architecture (2 terminals → 1 StoreHub)
- PostgreSQL transaction locking
- Invoice number generation strategy
- Inventory movement pattern
- Why NOT separate databases

**Use this when:** Understanding multi-terminal safety for Epic C

---

### 10. .NET Aspire Development Environment ✨ NEW
**File:** `10-aspire-dev-environment.md`

**Contents:**
- Aspire orchestration architecture
- AppHost configuration example
- ServiceDefaults project
- Developer workflow
- Development vs Production comparison

**Use this when:** Setting up or understanding the dev environment (Epic C)

---

### 11. Solution Structure ✨ NEW
**File:** `11-solution-structure.md`

**Contents:**
- Project dependency graph
- Folder structure
- Dependency rules
- Legacy migration path
- Project references summary

**Use this when:** Understanding project organization and dependencies

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

### For Debugging
1. Trace through `02-data-flow.md` to find where things break
2. Review `05-sync-flow-outbox-pattern.md` for sync issues
3. Check `04-database-schema.md` for query problems

### For Onboarding New Developers
Read in this order:
1. `01-architecture-overview.md` (big picture)
2. `03-component-relationships.md` (code organization)
3. `06-epic-roadmap.md` (implementation plan)
4. Others as needed

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

┌═════════┐
│ Process │  Critical/emphasized component
└═════════┘

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
```

---

**Related Documentation:**
- Implementation Status: `../implementation-status.md`
- Project Context: `../../../CLAUDE.md`
- v1.4.0 Docs: `../IndyPOS_Docs_v1_4_0/`
