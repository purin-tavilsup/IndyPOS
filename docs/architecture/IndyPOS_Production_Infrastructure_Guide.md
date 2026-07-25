# IndyPOS Production Infrastructure Recommendation

> **Status:** Recommended target architecture\
> **Cloud Provider:** DigitalOcean\
> **Application:** IndyPOS Cloud Backend

------------------------------------------------------------------------

# Goals

This architecture is designed to:

-   Keep stores operational when the Internet is unavailable.
-   Minimize operational overhead.
-   Scale independently at the application and database layers.
-   Start with a low monthly cost while allowing future growth.

------------------------------------------------------------------------

# High-Level Architecture

``` text
                        Internet
                            │
                     HTTPS (443)
                            │
                 ┌──────────────────┐
                 │     CloudApi     │
                 │ ASP.NET Core API │
                 │ Docker Container │
                 └────────┬─────────┘
                          │
                    Private VPC
                          │
               ┌────────────────────┐
               │ Managed PostgreSQL │
               └────────────────────┘

                         ▲
                         │ Secure Sync
                         │
┌────────────────────────────────────────────────────┐
│                    Individual Store                │
│                                                    │
│ Desktop POS (WinForms)                             │
│         │                                          │
│         ▼                                          │
│      StoreHub API                                 │
│         │                                          │
│         ▼                                          │
│   Local PostgreSQL Database                        │
└────────────────────────────────────────────────────┘
```

------------------------------------------------------------------------

# Recommended DigitalOcean Resources

  Component   Recommendation
  ----------- -----------------------------------------------
  Compute     Basic Premium AMD Droplet
  CPU         1 vCPU
  Memory      2 GB RAM
  Disk        50 GB SSD
  Runtime     Docker
  Database    Managed PostgreSQL (smallest production tier)
  Network     Private VPC
  SSL         HTTPS
  Backups     Enabled

------------------------------------------------------------------------

# Why This Configuration?

## CloudApi

The CloudApi is responsible for:

-   Authentication
-   Store synchronization
-   Report APIs
-   Administration APIs
-   Background event processing

It is relatively lightweight because stores perform most work locally.

A **2 GB / 1 vCPU** Droplet is an excellent starting point.

------------------------------------------------------------------------

## Managed PostgreSQL

Instead of hosting PostgreSQL inside the Droplet:

**Advantages**

-   Automatic backups
-   Point-in-time recovery
-   Automatic upgrades
-   Better monitoring
-   Easier scaling
-   Database isolated from application failures

------------------------------------------------------------------------

# Networking

Create a **DigitalOcean VPC**.

Resources inside the VPC:

-   CloudApi Droplet
-   Managed PostgreSQL

Firewall recommendations:

  Port   Purpose
  ------ -------------------------------
  443    Public HTTPS
  22     SSH (trusted IPs only)
  5432   PostgreSQL (VPC/private only)

------------------------------------------------------------------------

# Store Architecture

Each store contains:

``` text
Desktop POS
      │
StoreHub
      │
Local PostgreSQL
```

Benefits:

-   Offline sales continue.
-   Local performance remains fast.
-   Synchronization resumes automatically after Internet recovery.

------------------------------------------------------------------------

# Scaling Roadmap

## Phase 1 (Pilot)

-   1 × 2 GB / 1 vCPU Droplet
-   Smallest Managed PostgreSQL

Suitable for: - Development - Pilot deployments - Small number of stores

------------------------------------------------------------------------

## Phase 2

Resize the Droplet:

-   4 GB RAM
-   2 vCPUs

No application changes required.

------------------------------------------------------------------------

## Phase 3

Resize the Managed PostgreSQL cluster.

Suitable when:

-   More stores are added.
-   Reporting workload increases.
-   Synchronization volume grows.

------------------------------------------------------------------------

## Phase 4

``` text
                  Load Balancer
                        │
        ┌───────────────┴───────────────┐
        │                               │
  CloudApi Instance 1            CloudApi Instance 2
        │                               │
        └───────────────┬───────────────┘
                        │
                Managed PostgreSQL
```

------------------------------------------------------------------------

# Cost Optimization

## Start Small

-   2 GB / 1 vCPU Droplet
-   Smallest Managed PostgreSQL

Increase resources only when metrics indicate higher demand.

This keeps infrastructure costs low during the early stages.

------------------------------------------------------------------------

# Alternatives Considered

## Self-host PostgreSQL on the Droplet

Pros

-   Lowest monthly cost

Cons

-   Manual backups
-   Manual upgrades
-   Shared CPU/RAM with CloudApi
-   Harder disaster recovery

------------------------------------------------------------------------

## App Platform

Pros

-   Simpler deployments
-   Less server management

Cons

-   Less flexibility
-   Typically higher cost
-   Less control over runtime

For IndyPOS, a Droplet provides the best balance of flexibility, cost,
and operational simplicity.

------------------------------------------------------------------------

# Final Recommendation

## Cloud

-   Basic Premium AMD Droplet
    -   1 vCPU
    -   2 GB RAM
    -   50 GB SSD
-   Docker deployment
-   Managed PostgreSQL
-   Private VPC
-   Firewall enabled
-   Automatic database backups

## Store

-   Desktop POS
-   StoreHub
-   Local PostgreSQL

This architecture provides a solid foundation for IndyPOS while
remaining inexpensive to operate initially and easy to scale as more
stores are onboarded.
