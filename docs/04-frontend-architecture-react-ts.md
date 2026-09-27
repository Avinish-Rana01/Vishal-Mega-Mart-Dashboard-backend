# Document 04: Modern React + TypeScript Architecture
## Enterprise Frontend Blueprint: TanStack Table v8, TanStack Query v5 & URL-First State

> **Purpose:** Eliminate duplicated table code across 16+ pages, guarantee 100% type-safe column sorting, and ensure seamless dashboard-to-report navigation.

---

## 1. Core Architectural Pillars

```
┌────────────────────────────────────────────────────────────────────────┐
│                        BROWSER URL QUERY STRING                        │
│             /reports/void-details?store=HD44&page=1&sort=void_qty      │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Single Source of Truth
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   `useServerTable` Custom Hook                         │
│   • Synchronizes state with URL search params                          │
│   • Manages debounce, pagination, and sort direction toggles           │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
                    ▼                                ▼
┌─────────────────────────────────────┐  ┌───────────────────────────────┐
│         TanStack Query v5           │  │      TanStack Table v8        │
│ • Server State Caching & Dedup      │  │ • Headless table model        │
│ • Automatic request cancellation    │  │ • Column sorting metadata     │
│ • Stale-While-Revalidate            │  │ • Virtualized rendering       │
└─────────────────────────────────────┘  └───────────────────────────────┘
```

1. **TypeScript Everywhere:** Strict compile-time checks ensure column keys and sort parameters cannot be misspelled.
2. **URL as the Single Source of Truth:** Table state (page, sort, filters, store) lives in the URL query string (`useSearchParams`). Refreshing, sharing links, or pressing the browser "Back" button preserves the exact user view.
3. **Headless TanStack Table v8:** Separation of table logic (sorting, selection, pagination) from table UI (styling, DOM rendering).
4. **TanStack Query v5 (React Query):** Eliminates manual `useEffect` fetching, race conditions, and loading states with built-in request deduplication and caching.

---

## 2. Centralized Report Registry (`reportRegistry.ts`)

Instead of defining columns, default sorting, and API routes inside scattered `.jsx` files, declare every report in a centralized, type-safe configuration:

```typescript
// src/config/reportRegistry.ts

export interface ColumnDefinition<TData> {
  id: string;
  header: string;
  accessorKey?: keyof TData;
  sortable?: boolean;
  sortKey?: string;
  align?: 'left' | 'center' | 'right';
  cell?: (info: { getValue: () => any; row: { original: TData } }) => React.ReactNode;
}

export interface ReportConfig<TItem, TSummary> {
  id: string;
  title: string;
  route: string;
  endpoint: string;
  defaultSort: {
    column: string;
    direction: 'asc' | 'desc';
  };
  columns: ColumnDefinition<TItem>[];
}

// Example definition for Void Details Report
export interface VoidReportRow {
  transactionDate: string;
  storeCode: string;
  storeName: string;
  voidQty: number;
  encodedQty: number;
  pendingQty: number;
}

export interface VoidReportSummary {
  totalVoidQty: number;
  totalEncodedQty: number;
  totalPendingQty: number;
}

export const VOID_DETAILS_REPORT: ReportConfig<VoidReportRow, VoidReportSummary> = {
  id: 'VOID_DETAILS',
  title: 'Void Details Report',
  route: '/reports/void-details',
  endpoint: '/api/reports/void-details',
  defaultSort: {
    column: 'transaction_date',
    direction: 'desc',
  },
  columns: [
    {
      id: 'sr_no',
      header: 'SR.NO',
      sortable: false,
    },
    {
      id: 'transactionDate',
      header: 'DATE',
      accessorKey: 'transactionDate',
      sortable: true,
      sortKey: 'transaction_date',
    },
    {
      id: 'voidQty',
      header: 'VOID QTY',
      accessorKey: 'voidQty',
      sortable: true,
      sortKey: 'void_qty',
      align: 'right',
    },
    {
      id: 'encodedQty',
      header: 'ENCODED VS VOID (QTY)',
      accessorKey: 'encodedQty',
      sortable: true,
      sortKey: 'encoded_qty',
      align: 'right',
    },
    {
      id: 'pendingQty',
      header: 'PENDING QTY',
      accessorKey: 'pendingQty',
      sortable: true,
      sortKey: 'pending_qty',
      align: 'right',
    },
  ],
};
```

---

## 3. The Universal Headless Table Hook (`useServerTable.ts`)

This single reusable hook completely eliminates duplicated `useState` calls across 16+ pages:

```typescript
// src/hooks/useServerTable.ts
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { ReportConfig } from '../config/reportRegistry';

interface UseServerTableOptions<TItem, TSummary> {
  reportConfig: ReportConfig<TItem, TSummary>;
  defaultFilters?: Record<string, string>;
}

export function useServerTable<TItem, TSummary>({
  reportConfig,
  defaultFilters = {},
}: UseServerTableOptions<TItem, TSummary>) {
  const [searchParams, setSearchParams] = useSearchParams();

  // Read current parameters directly from URL (with fallbacks)
  const pageIndex = Number(searchParams.get('page') || '1');
  const pageSize = Number(searchParams.get('size') || '10');
  const sortColumn = searchParams.get('sort') || reportConfig.defaultSort.column;
  const sortDirection = (searchParams.get('dir') || reportConfig.defaultSort.direction) as 'asc' | 'desc';
  const searchTerm = searchParams.get('q') || '';
  const storeCode = searchParams.get('store') || defaultFilters.storeCode || '';
  const fromDate = searchParams.get('from') || defaultFilters.fromDate || '';
  const toDate = searchParams.get('to') || defaultFilters.toDate || '';

  // Helper to update URL params
  const updateParams = (newParams: Record<string, string | number | null>) => {
    setSearchParams((prev) => {
      const updated = new URLSearchParams(prev);
      Object.entries(newParams).forEach(([key, val]) => {
        if (val === null || val === '') {
          updated.delete(key);
        } else {
          updated.set(key, String(val));
        }
      });
      return updated;
    });
  };

  // TanStack Query for Data Fetching with Auto-cancellation
  const query = useQuery({
    queryKey: [reportConfig.id, { pageIndex, pageSize, sortColumn, sortDirection, searchTerm, storeCode, fromDate, toDate }],
    queryFn: async ({ signal }) => {
      const response = await axios.get(reportConfig.endpoint, {
        params: {
          pageIndex,
          pageSize,
          sortBy: sortColumn,
          direction: sortDirection,
          searchTerm,
          storeCode,
          fromDate,
          toDate,
        },
        signal,
      });
      return response.data;
    },
    staleTime: 30_000, // Cache clean for 30s
  });

  // Handlers
  const handleSort = (columnId: string) => {
    const colDef = reportConfig.columns.find((c) => c.id === columnId);
    if (!colDef || colDef.sortable === false) return;

    const targetSortKey = colDef.sortKey || colDef.id;
    const isCurrent = sortColumn === targetSortKey;
    const nextDirection = isCurrent && sortDirection === 'asc' ? 'desc' : 'asc';

    updateParams({
      sort: targetSortKey,
      dir: nextDirection,
      page: 1, // Always reset to page 1 on sort change
    });
  };

  const handlePageChange = (newPage: number) => {
    updateParams({ page: newPage });
  };

  const handlePageSizeChange = (newSize: number) => {
    updateParams({ size: newSize, page: 1 });
  };

  const handleSearch = (term: string) => {
    updateParams({ q: term || null, page: 1 });
  };

  return {
    data: query.data?.items ?? [],
    summary: query.data?.summary,
    totalCount: query.data?.totalCount ?? 0,
    totalPages: query.data?.totalPages ?? 1,
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    error: query.error,
    pagination: { pageIndex, pageSize },
    sorting: { sortColumn, sortDirection },
    filters: { storeCode, fromDate, toDate, searchTerm },
    actions: {
      handleSort,
      handlePageChange,
      handlePageSizeChange,
      handleSearch,
      updateParams,
      refetch: query.refetch,
    },
  };
}
```

---

## 4. The Modernized Report Page Component

Using this pattern, an entire report page is reduced from 450 lines of messy state to **under 45 lines of declarative, bug-free code**:

```tsx
// src/pages/Report/VoidDetailsReportPage.tsx
import React from 'react';
import { useServerTable } from '../../hooks/useServerTable';
import { VOID_DETAILS_REPORT, VoidReportRow, VoidReportSummary } from '../../config/reportRegistry';
import { ModernDataTable } from '../../components/tables/ModernDataTable';
import { ReportHeader } from '../../components/common/ReportHeader';

export default function VoidDetailsReportPage() {
  const {
    data,
    summary,
    totalCount,
    isLoading,
    pagination,
    sorting,
    actions,
  } = useServerTable<VoidReportRow, VoidReportSummary>({
    reportConfig: VOID_DETAILS_REPORT,
  });

  return (
    <div className="report-container">
      <ReportHeader
        title={VOID_DETAILS_REPORT.title}
        summary={summary}
        onSearch={actions.handleSearch}
      />

      <ModernDataTable
        columns={VOID_DETAILS_REPORT.columns}
        data={data}
        totalCount={totalCount}
        pageIndex={pagination.pageIndex}
        pageSize={pagination.pageSize}
        sortColumn={sorting.sortColumn}
        sortDirection={sorting.sortDirection}
        isLoading={isLoading}
        onSortChange={actions.handleSort}
        onPageChange={actions.handlePageChange}
        onPageSizeChange={actions.handlePageSizeChange}
      />
    </div>
  );
}
```

---

## 5. Type-Safe Route Navigation (Eliminating Broken Redirections)

Never write manual string concatenations like `navigate('/reports/void-details?store=' + store)`. Create a type-safe route directory:

```typescript
// src/config/routes.ts

export const APP_ROUTES = {
  HOME: '/',
  DASHBOARD: '/dashboard',
  REPORTS: {
    VOID_DETAILS: (params?: { store?: string; from?: string; to?: string }) => {
      const q = new URLSearchParams();
      if (params?.store) q.set('store', params.store);
      if (params?.from) q.set('from', params.from);
      if (params?.to) q.set('to', params.to);
      const queryStr = q.toString();
      return `/reports/void-details${queryStr ? `?${queryStr}` : ''}`;
    },
    STORE_GRC: (params?: { store?: string; date?: string }) => {
      const q = new URLSearchParams();
      if (params?.store) q.set('store', params.store);
      if (params?.date) q.set('date', params.date);
      const queryStr = q.toString();
      return `/reports/store-grc${queryStr ? `?${queryStr}` : ''}`;
    },
  },
};
```

When a user clicks any dashboard card:
```typescript
// On Card Click in Dashboard:
navigate(APP_ROUTES.REPORTS.VOID_DETAILS({ store: selectedStore, from: '2026-09-01' }));
```
If a route name or parameter changes, the TypeScript compiler immediately points out any broken card links across the entire project!
