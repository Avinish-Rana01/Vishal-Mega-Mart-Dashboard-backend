# Document 06: Pragmatic Incremental Migration Plan
## Modernizing the Live Enterprise Dashboard Without Halting Business Operations

> **Purpose:** A structured, low-risk roadmap to evolve the existing VMM Dashboard into the target architecture step-by-step without breaking office workflows or disrupting daily users.

---

## 1. Migration Strategy: The Strangler Fig Pattern

Never attempt a "Big Bang" rewrite of an active enterprise system. Rewriting everything from scratch in a separate repository almost always fails or takes months longer than expected while business requirements continue to change.

Instead, modern teams apply the **Strangler Fig Pattern**:
We build the new architectural patterns (testing, centralized schemas, modular hooks) directly inside the existing project, migrating **one report at a time** while the rest of the application continues running in production.

```
PHASE 1: Safety Net          PHASE 2: Client Standards     PHASE 3: Backend Modularization    PHASE 4: Full CI/CD
┌─────────────────────────┐  ┌─────────────────────────┐  ┌───────────────────────────────┐  ┌────────────────────────┐
│ • Playwright E2E Tests  │─>│ • Central ReportRegistry│─>│ • Extract 1 SP per Report     │─>│ • Full TypeScript      │
│ • Contract Audit Script │  │ • Unified useServerTable│  │ • Strongly Typed Enums & DTOs │  │ • OpenAPI Codegen      │
│ • Branch protection     │  │ • URL SearchParams sync │  │ • FluentValidation Gate       │  │ • GitHub Actions Gate  │
└─────────────────────────┘  └─────────────────────────┘  └───────────────────────────────┘  └────────────────────────┘
       Week 1                       Weeks 2 – 3                      Weeks 4 – 5                    Weeks 6 – 7
```

---

## 2. Phase-by-Phase Execution Plan

### Phase 1: Establish the Automated Safety Net (Week 1)
**Objective:** Never manually test 16 pages again. Catch all future regressions in 60 seconds.

- [x] **Audit Script in Repository:** Place the automated Python contract auditor in `scripts/audit-contracts.py` to ensure all frontend `sortKey` attributes match the database stored procedures.
- [ ] **Install Playwright:** Install Playwright in `Vishal-Mega-Mart-Dashboard-V2`.
- [ ] **Implement 4 Core User Journeys:**
  1. Dashboard card click ➔ Verify redirect URL & filter prefill.
  2. Table sort click ➔ Verify network request parameters and sort indicator.
  3. Row click ➔ Verify drilldown modal opens with data.
  4. Export button click ➔ Verify Excel download triggers.
- [ ] **Git Branch Hygiene:** Create dedicated feature branches (`feature/report-x`, `fix/sorting-y`) and stop committing directly to `main`.

---

### Phase 2: Frontend Standardization (Weeks 2 – 3)
**Objective:** Eliminate 80% of duplicated frontend code and make all table interactions bulletproof.

- [ ] **Create `src/config/reportRegistry.js`:**
  Consolidate column definitions, titles, endpoints, and default sort configurations for all 16 reports into a single file.
- [ ] **Implement Universal `useServerTable.js` Hook:**
  Standardize pagination, sort toggling, search debounce, and URL query string synchronization into a single reusable hook.
- [ ] **Migrate Reports One by One:**
  Convert reports to use `useServerTable` starting with the most frequently used:
  1. `StoreGrcReportPage.jsx`
  2. `VoidDetailsReportPage.jsx`
  3. `ReturnDetailsReportPage.jsx`
  4. `CycleCountReportPage.jsx`
- [ ] **Type-Safe Navigation Helpers:**
  Create `src/config/routes.js` to ensure dashboard cards use structured route functions instead of string concatenation.

---

### Phase 3: Backend & Database Modularization (Weeks 4 – 5)
**Objective:** Break apart `SP_NEW_REPORT` and enforce strong typing on the API.

- [ ] **Carve Out Dedicated Stored Procedures:**
  For each report, extract its block from `SP_NEW_REPORT` into an isolated procedure:
  - Extract `LAST7DAY_VOID_DASHBOARD` ➔ `usp_Report_VoidDetails`
  - Extract `LAST7DAY_RETURN_DASHBOARD` ➔ `usp_Report_ReturnDetails`
  - Extract `STORE_GRC_REPORT` ➔ `usp_Report_StoreGrcSummary`
- [ ] **Standardize Column Names:**
  Remove spaces in column names in new procedures (`void_qty` instead of `[VOID QTY]`).
- [ ] **Adopt Vertical Slices in .NET:**
  Group report files by feature folder (`Features/Reports/VoidDetails/` with query, validator, and handler).
- [ ] **Implement FluentValidation:**
  Reject invalid dates, negative page numbers, or unhandled sort fields before querying SQL Server.

---

### Phase 4: Full Contract Generation & CI/CD Pipeline (Weeks 6 – 7)
**Objective:** End-to-end type safety and automated deployment gates.

- [ ] **Generate Frontend Types from Swagger:**
  Add `npm run codegen` using `openapi-typescript` or `Orval` to automatically create TypeScript interfaces from .NET Swagger.
- [ ] **Configure GitHub Actions CI Workflow:**
  Ensure that every Pull Request automatically runs linter, build, contract audit, and Playwright tests before merge approval.

---

## 3. Immediate Action Checklist for Your Team Today

| Priority | Task | Target File | Impact |
| :--- | :--- | :--- | :--- |
| **P0 (Immediate)** | Revert unintended edits in `DashboardPage.jsx` (hidden sections) | `src/pages/Dashboard/DashboardPage.jsx` | Prevents accidentally showing unfinished sections in production |
| **P0 (Immediate)** | Isolate verified sorting fixes onto a feature branch | `git checkout -b fix/align-report-table-sorting` | Keeps `main` completely clean and safe |
| **P1 (High)** | Install Playwright & run initial smoke test | `package.json`, `playwright.config.ts` | Instant peace of mind across all pages |
| **P1 (High)** | Create `src/config/reportRegistry.js` | `src/config/reportRegistry.js` | Single source of truth for all column keys |
| **P2 (Medium)** | Replace manual state with `useServerTable` on 1 report | `src/pages/Report/StoreGrcReportPage.jsx` | Proves the pattern with zero risk |
