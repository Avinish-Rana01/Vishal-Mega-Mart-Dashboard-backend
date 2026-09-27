# Vishal Mega Mart (VMM) RFID Retail Dashboard
## Enterprise Architecture Blueprint: Re-Engineering for Scale, Reliability & Maintainability

> **Document Type:** Production Architecture Specification & Re-Engineering Master Plan  
> **Target Audience:** Engineering Leads, Full-Stack Developers, Database Administrators, Product Owners  
> **Status:** Approved Architecture Blueprint  
> **Target Tech Stack:** .NET 8/9 Web API, Microsoft SQL Server, React 19 + TypeScript + Vite, TanStack Table v8, TanStack Query v5, Playwright E2E  

---

## 1. Executive Summary

The **Vishal Mega Mart (VMM) RFID Retail Solution** is a high-volume retail operations platform that manages RFID encoding, store goods receiving (GRC), cycle counts, point-of-sale checkout reconciliation, DC validation, vendor discrepancies, and live stock tracking across hundreds of retail stores.

As the platform has expanded to over **16 complex reports, 100+ data table columns, real-time SignalR sockets, drill-down modals, and multi-format streaming exports**, the development overhead and regression risk have multiplied exponentially:
- Minor changes in database stored procedures frequently break table sorting silently.
- Dashboard card redirections occasionally lose filter state on browser refresh.
- Developers are forced to manually test hundreds of click paths, sort headers, and export buttons before every release.

This documentation suite provides a **complete, end-to-end blueprint for re-engineering the application from scratch**, highlighting the anti-patterns in the legacy implementation and providing production-grade, battle-tested solutions for the database, backend, and frontend.

---

## 2. Blueprint Navigation (Documentation Suite)

| Document | Focus Area | Key Topics Covered |
| :--- | :--- | :--- |
| [**01 - Current System Audit & Anti-Patterns**](./01-current-system-audit-and-antipatterns.md) | Legacy Codebase Analysis | Real findings from `SP_NEW_REPORT`, pass-through controllers, duplicated frontend state, and silent failure modes. |
| [**02 - Database Redesign Guide**](./02-database-redesign-guide.md) | SQL Server & Data Layer | Eliminating God Stored Procedures, parameterized dynamic sorting without silent fallbacks, composite indexing for retail date-ranges. |
| [**03 - Backend Architecture (.NET 8/9)**](./03-backend-architecture-net8.md) | ASP.NET Core Web API | Vertical Slice Architecture, CQRS with MediatR, FluentValidation, generic paged result models, resilient SignalR hubs. |
| [**04 - Frontend Architecture (React + TS)**](./04-frontend-architecture-react-ts.md) | Client Architecture | TypeScript transition, Headless TanStack Table v8, TanStack Query v5 caching, URL-as-Single-Source-of-Truth, Centralized Report Registry. |
| [**05 - Contract-First & Testing Suite**](./05-end-to-end-contracts-and-testing.md) | Automation & QA | Automated Playwright E2E test suites (cards, redirects, sort, exports), OpenAPI-to-TypeScript code generation, CI/CD pipeline. |
| [**06 - Incremental Migration Plan**](./06-incremental-migration-plan.md) | Real-World Execution | Pragmatic 4-phase transition plan to modernize the live office project without halting ongoing business operations. |

---

## 3. High-Level Target System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                CLIENT TIER (React 19 + TypeScript)                      │
│                                                                                         │
│   ┌─────────────────────┐   ┌───────────────────────┐   ┌───────────────────────────┐   │
│   │ URL SearchParams    │   │ TanStack Query v5     │   │ TanStack Table v8         │   │
│   │ (Single Truth State)│ ──│ (Cache, Retry, Dedup) │ ──│ (Virtualized Headless Grid│   │
│   └─────────────────────┘   └───────────────────────┘   └───────────────────────────┘   │
│                                       │                                                 │
│                                       ▼                                                 │
│                     ┌──────────────────────────────────┐                                │
│                     │ Auto-Generated API Client (Orval)│                                │
│                     └──────────────────────────────────┘                                │
└───────────────────────────────────────┬─────────────────────────────────────────────────┘
                                        │ HTTPS / WSS (SignalR)
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                APPLICATION TIER (.NET 8/9 Web API)                      │
│                                                                                         │
│   ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│   │ Vertical Slice Features: /Features/Reports/{ReportName}/                        │   │
│   │   ├── Endpoint (FastEndpoints / Minimal APIs / Controller)                      │   │
│   │   ├── Query & Response DTOs (Strongly Typed, No Loose Strings)                  │   │
│   │   ├── FluentValidation Rules (Pre-execution validation)                         │   │
│   │   └── Query Handler (Dapper / EF Core compiled queries)                         │   │
│   └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                       │                                                 │
│                                       ▼                                                 │
│                     ┌──────────────────────────────────┐                                │
│                     │ Centralized Stored Proc Gateway  │                                │
│                     └──────────────────────────────────┘                                │
└───────────────────────────────────────┬─────────────────────────────────────────────────┘
                                        │ Parameterized Queries / Validated Stored Procs
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                DATABASE TIER (Microsoft SQL Server)                     │
│                                                                                         │
│   ┌─────────────────────────────────────────────────────────────────────────────────┐   │
│   │ Dedicated Single-Responsibility Procedures:                                     │   │
│   │   ├── usp_Report_StoreGrcSummary                                                │   │
│   │   ├── usp_Report_VoidReconciliation                                             │   │
│   │   └── usp_Report_CycleCountVariance                                             │   │
│   │                                                                                 │   │
│   │ Strict Snake_Case naming, whitelisted sorting schemas, composite date indexes   │   │
│   └─────────────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Key Tenets of the New Design

1. **Zero Runtime Surprises (Strict Type Safety):**
   - No string identifiers passed across network boundaries without schema enforcement.
   - The OpenAPI contract generated by the backend is the authoritative source that generates frontend types.
2. **Automated Verification Over Human Memory:**
   - Every single interactive action (card navigation, table sort, modal toggle, excel export) is backed by an automated **Playwright E2E test** that runs in seconds.
3. **URL as Single Source of Truth:**
   - Active store filters, date ranges, page numbers, and sort columns are always reflected in the URL query string (`?store=HD44&sort=GRC_DATE&dir=desc`). Browser refresh, bookmarking, and link sharing work identically every time.
4. **Decoupled, Single-Responsibility Services:**
   - Replacing 4,500-line monolithic stored procedures with isolated, single-purpose database procedures with dedicated indexes.
5. **Zero Boilerplate Frontend:**
   - Implementing a universal headless table hook (`useServerTable`) powered by TanStack Table, reducing each report page from 400+ lines of duplicate state to a clean, declarative 40-line configuration.
