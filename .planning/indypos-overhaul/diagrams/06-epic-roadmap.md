# Implementation Roadmap - Epic Dependencies

Version: 1.1.0
Date: 2026-03-08

## Epic Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│  LEGEND                                                         │
│  ══════                                                         │
│  ┌────────┐                                                     │
│  │Epic    │  Epic ready to start                               │
│  └────────┘                                                     │
│                                                                 │
│  [Epic]     Epic blocked (depends on others)                   │
│                                                                 │
│  ────────►  Dependency (must complete first)                   │
└─────────────────────────────────────────────────────────────────┘


                        START
                          │
                          ▼
                  ╔═══════════════╗
                  ║   Epic 0  ✅  ║  Sprint 1
                  ║   Extract     ║  PRIORITY: HIGH
                  ║   Business    ║  ✅ COMPLETE
                  ║   Logic       ║
                  ╚═══════╤═══════╝
                          │
                          │ Enables clean separation
                          ▼
         ┌────────────────┴────────────────┐
         │                                 │
         ▼                                 ▼
╔═══════════════╗               ╔═══════════════╗
║   Epic B  ✅  ║               ║   Epic A  ✅  ║  Sprint 1
║   Remove      ║               ║   Prepare     ║  PRIORITY: HIGH
║   Deprecated  ║               ║   Codebase    ║  ✅ COMPLETE
║   PG Report   ║               ║   (StoreId)   ║
╚═══════╤═══════╝               ╚═══════╤═══════╝
         │                               │
         └───────────────┬───────────────┘
                         │ Both enable
                         ▼
                  ╔═══════════════╗
                  ║   Epic D  ✅  ║  Sprint 1
                  ║   Schema      ║  PRIORITY: HIGH
                  ║   Design      ║  ✅ COMPLETE
                  ║   (PublicId)  ║
                  ╚═══════╤═══════╝
                          │
                          │ Required for
                          ▼
                  ┌───────────────┐
                  │   Epic C      │  Sprint 2       ◄── WE ARE HERE
                  │   StoreHub    │  PRIORITY: HIGH
                  │   + Aspire    │  PR Count: ~7
                  │   Service     │  Risk: MEDIUM
                  └───────┬───────┘
                          │
                          │ Enables
                          ▼
         ┌────────────────┴────────────────┐
         │                                 │
         ▼                                 ▼
┌─────────────────┐             ┌─────────────────┐
│   Epic E        │             │   Epic F        │  Sprint 3-4
│   Outbox +      │             │   Cloud API     │  PRIORITY: MEDIUM
│   SyncWorker    │             │   (Singapore)   │  PR Count: ~7
│   (Background)  │             │   + DB          │  Risk: MEDIUM
│                 │             │                 │
└────────┬────────┘             └────────┬────────┘
         │                               │
         │                               │
         └───────────┬───────────────────┘
                     │ Both required for
                     ▼
             ┌───────────────┐
             │   Epic G      │  Sprint 5
             │   Desktop     │  PRIORITY: MEDIUM
             │   Integration │  PR Count: ~4
             │   (Client)    │  Risk: LOW
             └───────┬───────┘
                     │
                     │ Enables
                     ▼
             ┌───────────────┐
             │   Epic H      │  Sprint 6
             │   Testing &   │  PRIORITY: HIGH
             │   Rollout     │  PR Count: ~8
             │   (Production)│  Risk: HIGH
             └───────┬───────┘
                     │
                     ▼
                    END
```

---

## Sprint Breakdown

```
Sprint 1 (Weeks 1-2)
═══════════════════════════════════════════════════════════
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│   Epic B    │ → │   Epic A    │ → │   Epic D    │
│   (Remove)  │   │   (Prepare) │   │   (Schema)  │
└─────────────┘   └─────────────┘   └─────────────┘

Goals:
  ✓ Clean codebase (remove deprecated)
  ✓ Add StoreId infrastructure
  ✓ Define PostgreSQL schema
  ✓ Create migration templates

Deliverables:
  • 12 PRs total
  • Schema documented and reviewed
  • Dev environment ready (docker-compose)

Risk: LOW
Blockers: None


Sprint 2 (Weeks 3-4)
═══════════════════════════════════════════════════════════
┌─────────────┐
│   Epic C    │
│  (StoreHub) │
└─────────────┘

Goals:
  ✓ Create StoreHub Windows Service project
  ✓ EF Core + local Postgres working
  ✓ Basic endpoints functional
  ✓ Can complete a sale via API

Deliverables:
  • 6 PRs
  • StoreHub runs as Windows Service
  • Integration tests passing

Risk: MEDIUM (new project setup)
Blockers: Epic D must be complete


Sprint 3 (Weeks 5-6)
═══════════════════════════════════════════════════════════
┌─────────────┐   ┌─────────────┐
│   Epic E    │   │   Epic F    │
│  (Outbox)   │   │   (Cloud)   │
└─────────────┘   └─────────────┘

Goals:
  ✓ Outbox pattern implemented
  ✓ SyncWorker background service
  ✓ Cloud API deployed (Singapore)
  ✓ Idempotent event ingestion

Deliverables:
  • 12 PRs total
  • End-to-end sync working
  • Offline → reconnect → sync verified

Risk: MEDIUM (distributed systems)
Blockers: Epic C must be complete


Sprint 4 (Week 7)
═══════════════════════════════════════════════════════════
┌─────────────┐
│   Epic F    │
│  (Cloud)    │
│  continued  │
└─────────────┘

Goals:
  ✓ Master data endpoints
  ✓ API authentication
  ✓ Reporting endpoints

Deliverables:
  • 5 PRs
  • Master data sync working
  • Basic reports available

Risk: LOW
Blockers: Epic F Part 1


Sprint 5 (Week 8)
═══════════════════════════════════════════════════════════
┌─────────────┐
│   Epic G    │
│ (Desktop    │
│  Client)    │
└─────────────┘

Goals:
  ✓ Desktop app calls StoreHub API
  ✓ Decommission direct SQLite writes
  ✓ Prepare for tablet support

Deliverables:
  • 4 PRs
  • Desktop fully uses StoreHub
  • Old SQLite code removed

Risk: LOW
Blockers: Epics E & F


Sprint 6 (Weeks 9-10)
═══════════════════════════════════════════════════════════
┌─────────────┐
│   Epic H    │
│ (Testing &  │
│  Rollout)   │
└─────────────┘

Goals:
  ✓ Comprehensive test suite
  ✓ Migration from SQLite tested
  ✓ Pilot store deployment
  ✓ Operational runbook

Deliverables:
  • 8 PRs (tests + docs)
  • Store 1 live and monitored
  • Rollout plan for stores 2-3

Risk: HIGH (production deployment)
Blockers: All previous epics
```

---

## Critical Path

```
The minimum path to get ONE store live:

  Epic B → Epic A → Epic D → Epic C → Epic E → Epic F → Epic G → Epic H
  (2 wks)  (1 wk)   (2 wks)  (2 wks)  (2 wks)  (2 wks)  (1 wk)   (2 wks)

Total: ~10 weeks (2.5 months) for full rollout
```

---

## Parallel Work Opportunities

```
Week 5-6 (Sprint 3):
┌─────────────────────────────────────────────────────┐
│                                                     │
│  Developer A:  Epic E (Outbox + SyncWorker)         │
│                StoreHub side                        │
│                                                     │
│  Developer B:  Epic F (Cloud API)                   │
│                Cloud side                           │
│                                                     │
│  These can happen in parallel! 🚀                   │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## Epic Details & Dependencies

### Epic B: Remove Deprecated PG Report
**Dependencies:** None
**Enables:** Clean slate for Epic A

Tasks:
```
B1 ─► B2 ─► B3 ─► B4 ─► B5
 │           │     │
 │           │     └─► Can be parallel
 │           └──────► Can be parallel
 └──────────────────► Must be first
```

### Epic A: Prepare Codebase
**Dependencies:** Epic B (nice to have, not blocker)
**Enables:** Epic D

Tasks:
```
A1 (docs)    ─► Independent
A2 (StoreId) ─► Needed for Epic D
A3 (dev env) ─► Needed for Epic C
```

### Epic D: Schema Design
**Dependencies:** Epic A (for StoreId concept)
**Enables:** Epic C

Tasks:
```
D1 (PublicId) ─┐
D2 (Outbox)   ─┤─► Can be parallel
D3 (Inventory)─┘
D4 (Migrations) ─► Depends on D1-D3
```

### Epic C: StoreHub Service
**Dependencies:** Epic D (schema must be defined)
**Enables:** Epics E & F

Tasks:
```
C1 (Project) ─► C2 (EF Core) ─► C3 (Endpoints) ─► C4 (Logic)
```

### Epic E: Outbox + SyncWorker
**Dependencies:** Epic C
**Enables:** Epic G

Tasks:
```
E1 (Outbox table) ─► E2 (Write events) ─┐
                                        ├─► E4 (Observability)
E3 (SyncWorker)   ──────────────────────┘
```

### Epic F: Cloud API
**Dependencies:** Epic C
**Enables:** Epic G

Tasks:
```
F1 (Project) ─► F2 (Idempotency) ─► F3 (Process events)
                                    ├─► F4 (Master data)
                                    └─► F5 (Auth)
```

### Epic G: Desktop Integration
**Dependencies:** Epics E & F (sync must work end-to-end)
**Enables:** Epic H

Tasks:
```
G1 (API client) ─► G2 (LAN prep) ─► G3 (Remove SQLite)
```

### Epic H: Testing & Rollout
**Dependencies:** Epic G (full system working)
**Enables:** Production!

Tasks:
```
H1 (Tests) ────────┐
H2 (Upgrade test) ─┤─► H3 (Pilot) ─► H4 (Runbook)
```

---

## Risk Mitigation

```
┌────────────────────────────────────────────────────────────┐
│  High Risk Points                                          │
├────────────────────────────────────────────────────────────┤
│  1. Epic D (Schema Migration)                              │
│     Risk: Data loss during SQLite → Postgres migration     │
│     Mitigation:                                            │
│       • Test with production backups                       │
│       • Dry-run migrations                                 │
│       • Rollback plan                                      │
│                                                            │
│  2. Epic H (Pilot Rollout)                                 │
│     Risk: Store operations disrupted                       │
│     Mitigation:                                            │
│       • Deploy during low-traffic hours                    │
│       • Keep old system as fallback                        │
│       • On-site support during rollout                     │
│                                                            │
│  3. Epic F (Cloud Deployment)                              │
│     Risk: Cloud service downtime                           │
│     Mitigation:                                            │
│       • Staging environment first                          │
│       • Health checks + monitoring                         │
│       • Offline-first design (stores not blocked)          │
└────────────────────────────────────────────────────────────┘
```

---

## Success Criteria by Epic

```
Epic B: ✓ Build passes, no PG report code remains
Epic A: ✓ StoreId in config, docker-compose works
Epic D: ✓ Schema reviewed, migrations created
Epic C: ✓ StoreHub completes sale, stores in Postgres
Epic E: ✓ Events sync to cloud with retry
Epic F: ✓ Cloud receives and dedups events
Epic G: ✓ Desktop uses StoreHub exclusively
Epic H: ✓ Store 1 live, selling, syncing
```

---

## Current Status (as of 2026-03-03)

```
Epic B: 🔴 Not Started  (Next up!)
Epic A: 🔴 Not Started
Epic D: 🔴 Not Started
Epic C: 🔴 Not Started
Epic E: 🔴 Not Started
Epic F: 🔴 Not Started
Epic G: 🔴 Not Started
Epic H: 🔴 Not Started

Overall Progress: 0%
Ready to begin: Epic B 🚀
```

---

**See `.claude/implementation-status.md` for detailed task tracking**
