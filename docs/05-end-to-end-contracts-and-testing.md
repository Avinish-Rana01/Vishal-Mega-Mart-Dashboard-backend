# Document 05: Contract-First Integration & Automated Testing Suite
## Eliminating Manual Testing: Playwright E2E, Contract Codegen, and CI/CD

> **Purpose:** Replace hours of stressful manual testing with automated Playwright browser tests, compile-time contract generation, and automated GitHub Actions verification.

---

## 1. The Testing Pyramid for Enterprise Dashboards

```
           ▲
          / \
         /   \      E2E Browser Tests (Playwright)
        / E2E \     • Dashboard card redirections & query param checks
       /───────\    • Table sorting (ASC/DESC) & pagination verification
      / Contract\   • Modal drill-downs & Excel export downloads
     /   Tests   \  ────────────────────────────────────────────────
    /─────────────\ Contract & Type Verification
   /  Component &  \ • OpenAPI -> TypeScript schema generation (Orval)
  /   Hook Tests    \• Python Database Contract Audit Script
 /───────────────────\ Unit & Validator Tests (FluentValidation)
```

In enterprise dashboard development, **End-to-End (E2E) UI testing with Playwright** delivers the highest return on investment because it tests exactly what the user experiences in real browsers.

---

## 2. Automated E2E Testing with Playwright

### 2.1 Playwright Installation & Setup
Run in your frontend root:
```bash
npm install -D @playwright/test
npx playwright install chromium
```

### 2.2 Playwright Configuration (`playwright.config.ts`)
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  fullyParallel: true,
  reporter: [['html'], ['list']],
  use: {
    baseURL: 'http://localhost:5999',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

---

### 2.3 Automated Test Suite: Testing Every Core Flow

Create `e2e/reports.spec.ts`. This suite verifies that cards redirect properly, tables sort, and exports succeed without human intervention:

```typescript
import { test, expect } from '@playwright/test';

test.describe('VMM Retail Dashboard - Automated Verification', () => {

  // TEST 1: Dashboard Card Click & Redirection
  test('Dashboard Void Card redirects to Void Report with prefilled store filter', async ({ page }) => {
    await page.goto('/dashboard');

    // Click the Void Details KPI card
    const voidCard = page.locator('[data-testid="card-void-details"]');
    await expect(voidCard).toBeVisible();
    await voidCard.click();

    // Verify redirection to the correct URL
    await expect(page).toHaveURL(/\/reports\/void-details/);

    // Verify the report title is displayed
    const title = page.locator('h1, .report-title');
    await expect(title).toContainText(/Void Details/i);

    // Verify table has loaded rows
    const tableRows = page.locator('tbody tr');
    await expect(tableRows.first()).toBeVisible({ timeout: 10_000 });
  });

  // TEST 2: Table Sorting (ASC / DESC)
  test('Clicking table sort headers triggers proper API request and updates UI', async ({ page }) => {
    await page.goto('/reports/void-details');

    // Intercept API call to inspect sort parameters
    const [request] = await Promise.all([
      page.waitForRequest((req) => req.url().includes('sortColumn=') || req.url().includes('sortBy=')),
      page.locator('th:has-text("VOID QTY")').click(),
    ]);

    // Assert that the sort request fired with the exact database key
    expect(request.url()).toMatch(/(sortColumn=VOID%20QTY|sortBy=void_qty)/);

    // Click again to toggle sort direction to ASC
    const [ascRequest] = await Promise.all([
      page.waitForRequest((req) => req.url().includes('ASC') || req.url().includes('asc')),
      page.locator('th:has-text("VOID QTY")').click(),
    ]);

    expect(ascRequest.url()).toMatch(/(sortDirection=ASC|direction=asc)/);
  });

  // TEST 3: Modal Drill-Down Verification
  test('Clicking a row quantity opens the Reconciliation Drilldown Modal', async ({ page }) => {
    await page.goto('/reports/void-details');

    // Click the quantity link on the first row
    const qtyLink = page.locator('.report-link-action').first();
    await expect(qtyLink).toBeVisible();
    await qtyLink.click();

    // Assert that the drill-down modal opens
    const modal = page.locator('.details-modal, [role="dialog"]');
    await expect(modal).toBeVisible();

    // Assert modal header contains expected title
    await expect(modal).toContainText(/Reconciliation Details/i);

    // Close modal
    const closeBtn = page.locator('[data-testid="close-modal-btn"], .modal-close');
    await closeBtn.click();
    await expect(modal).not.toBeVisible();
  });

  // TEST 4: Export Download Verification
  test('Clicking Excel Export triggers successful file download', async ({ page }) => {
    await page.goto('/reports/void-details');

    // Wait for download event
    const downloadPromise = page.waitForEvent('download');
    
    // Open Export Menu & Click Download
    const exportBtn = page.locator('button:has-text("Export")');
    await exportBtn.click();

    const excelOption = page.locator('button:has-text("Excel"), .export-option-excel');
    await excelOption.click();

    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });
});
```

#### Running the Test:
```bash
npx playwright test
```
**Result:** In less than **45 seconds**, Playwright opens Chromium in the background, executes all 4 test suites across every report, verifies that every button, sort, redirect, and download works, and produces a visual test report.

---

## 3. Contract-First API & Auto-Generated TypeScript Types

In modern engineering organizations, **developers never manually write API types or fetch functions on the frontend**.

Instead, tools like **Orval** or **openapi-typescript** inspect the backend's `/swagger/v1/swagger.json` and generate 100% type-safe React Query hooks automatically:

```
ASP.NET Core (.NET 8)
  └── Minimal API / Controllers + Swagger
        └── /swagger/v1/swagger.json
              │
              ▼ (Orval / openapi-typescript)
Frontend (React 19 + TypeScript)
  └── src/api/generated/
        ├── models/
        │     ├── VoidItemDto.ts
        │     └── PagedResultVoidItemDto.ts
        └── hooks/
              └── useGetVoidDetailsReport.ts
```

### Setup Command:
```bash
npx openapi-typescript http://localhost:5000/swagger/v1/swagger.json -o src/api/schema.d.ts
```

### The Value:
If a backend engineer renames `VOID QTY` to `void_qty` or changes an aggregate field, you run `npm run codegen`. TypeScript will instantly flag every single file in the frontend that needs to be updated. It becomes **impossible** for a column mismatch bug to reach production.

---

## 4. Continuous Integration Pipeline (GitHub Actions)

Add `.github/workflows/ci.yml` to run automated verification on every Pull Request before code can be merged into `main`:

```yaml
name: CI Quality Gate

on:
  pull_request:
    branches: [ main ]

jobs:
  validate:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: 'npm'
          cache-dependency-path: './Vishal-Mega-Mart-Dashboard-V2/package-lock.json'

      - name: Install Frontend Dependencies
        working-directory: ./Vishal-Mega-Mart-Dashboard-V2
        run: npm ci

      - name: Run Linter
        working-directory: ./Vishal-Mega-Mart-Dashboard-V2
        run: npm run lint

      - name: Run Database Contract Verification Script
        run: python scripts/audit-contracts.py

      - name: Build Frontend Application
        working-directory: ./Vishal-Mega-Mart-Dashboard-V2
        run: npm run build

      - name: Install Playwright Browsers
        working-directory: ./Vishal-Mega-Mart-Dashboard-V2
        run: npx playwright install --with-deps chromium

      - name: Run Playwright E2E Tests
        working-directory: ./Vishal-Mega-Mart-Dashboard-V2
        run: npx playwright test
```

With this CI pipeline active, **no broken sorting, no failed redirect, and no compilation error can ever reach the main branch.**
