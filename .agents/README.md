# 🔬 Backend Full Optimization Report & Action Plan

> Complete analysis of every file in the VS Mart Backend codebase.  
> **Files analyzed**: 30+ files across Controllers, Services, Hubs, Models, and Configuration.

---

## Table of Contents
1. [🔴 Critical Issues (Fix Immediately)](#-critical-issues)
2. [🟠 High Priority Optimizations](#-high-priority-optimizations)
3. [🟡 Medium Priority Optimizations](#-medium-priority-optimizations)
4. [🟢 Low Priority / Nice-to-Haves](#-low-priority)
5. [✅ What's Already Done Well](#-whats-already-done-well)
6. [📊 Priority & Effort Matrix](#-priority-matrix)

---

## 🔴 Critical Issues

### 1. `SseNotifierService` + `EventsController` — Dead Code Wasting Resources

**Files**: [SseNotifierService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/SseNotifierService.cs) · [EventsController.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Controllers/EventsController.cs)

> [!CAUTION]
> This is a legacy SSE (Server-Sent Events) push system that was replaced by SignalR. **Nothing calls `NotifyRefresh()`**, so this entire system is dead code. Each connected client holds an open HTTP connection + semaphore in memory forever, consuming thread pool threads and memory for nothing.

- **Impact**: Thread pool starvation risk, memory leak  
- **Fix**: Delete both files entirely. Remove `SseNotifierService` from `Program.cs` DI registration.

---

### 2. `AuthService.Login()` — Sync-Over-Async Deadlock Bomb

**File**: [AuthService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Auth/AuthService.cs) (Lines 23–26)

```csharp
public LoginResponse Login(LoginRequest request)
{
    return LoginAsync(request).GetAwaiter().GetResult(); // ⚠️ DEADLOCK RISK
}
```

> [!CAUTION]
> `.GetAwaiter().GetResult()` blocks the calling thread and can cause a **deadlock** under ASP.NET's synchronization context. If any code path calls this synchronous method instead of `LoginAsync`, the entire thread pool can freeze.

- **Fix**: Delete this sync wrapper entirely. All controllers already call `LoginAsync`.

---

### 3. No JWT Authentication — Any Anonymous Request Gets Full Data

**Files**: [AuthController.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Auth/AuthController.cs) · [AuthService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Auth/AuthService.cs)

> [!WARNING]
> Login validates credentials against `SP_Master` but **returns zero tokens**. Every dashboard endpoint is completely unauthenticated. Any client can hit `/api/Stock/live-details?userId=26` to get Super Admin data without logging in. The `app.UseAuthorization()` middleware is present but **no `[Authorize]` attributes** exist on any controller.

- **Impact**: Full data exposure, no row-level security  
- **Fix**: Implement JWT issuance in `AuthService.LoginAsync`, add `[Authorize]` to all controllers, use claim-based `StoreCode` interception per the RBAC plan.

---

### 4. Hardcoded Super Admin User ID `26` Everywhere

**Files**: [CacheWarmerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/CacheWarmerService.cs#L43) · [LiveStockPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/LiveStockPollerService.cs#L156) · [DashboardSectionsPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/DashboardSectionsPollerService.cs#L213)

> [!WARNING]
> The pollers and cache warmer use `@User_ID = 26` (Super Admin) hardcoded. If this user is deleted or their permissions change, all real-time updates and cache warming silently return wrong data or zero rows.

- **Fix**: Move `SuperAdminUserId` to `appsettings.json` as a configuration key.

---

### 5. Hardcoded File Logging to `C:\publish\logs`

**Files**: [LiveStockPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/LiveStockPollerService.cs#L282-L287) · [DashboardSectionsPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/DashboardSectionsPollerService.cs#L748-L758)

```csharp
string logDir = @"C:\publish\logs";
File.AppendAllText(Path.Combine(logDir, "livestock_deltas.txt"), ...);
```

> [!WARNING]
> - **Synchronous `File.AppendAllText`** in a hot async path (called every 2 seconds) — blocks the thread pool.
> - **Hardcoded Windows path** — fails on Linux/Docker/Cloud deployment.
> - **No file rotation** — grows unbounded until disk fills.
> - **Redundant** — `ILogger` already captures the same info.

- **Fix**: Replace with `_logger.LogInformation(...)`. Delete the file writes entirely.

---

## 🟠 High Priority Optimizations

### 6. CycleCount Poller Fires **2 SQL Queries Per Tick**

**File**: [DashboardSectionsPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/DashboardSectionsPollerService.cs#L201-L329)

`PollCycleCountAsync` opens **two separate SQL connections** — one for `SP_New_Dashboard` (`CYCLE_COUNT_DASHBOARD`) and one for `SP_NEW_REPORT` (`CYCLE_COUNT_REPORT_VIEW`) — every 4 seconds.

- **Impact**: 2x connection overhead, 2x SQL Server load  
- **Fix**: Use a **single connection** with `QueryMultipleAsync`, or merge the lookup into a dictionary from `graphRows` before the `foreach` loop (avoid `O(n×m)` `FirstOrDefault`).

---

### 7. O(n²) Join in CycleCount — `FirstOrDefault` Inside `foreach`

**Files**: [DashboardSectionsPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/DashboardSectionsPollerService.cs#L261-L264) · [MainDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/MainDashboard/MainDashboardService.cs#L400-L412)

```csharp
var match = graphRows.FirstOrDefault(g =>
    string.Equals(refNo, GetString(g, "Ref_ID"), ...));
```

This runs inside a `foreach (var mainRow in items)`, creating **O(n × m)** complexity. With 100 stores × 100 graph rows = 10,000 comparisons every 4 seconds.

- **Fix**: Pre-build a `Dictionary<string, Dictionary<string, object?>>` from `graphRows` keyed by `Ref_ID` or `STORE_CODE`, then do O(1) lookup.

---

### 8. 60-Second SQL Timeout on a 2-Second Poller

**Files**: [LiveStockPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/LiveStockPollerService.cs#L145) · [DashboardSectionsPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/DashboardSectionsPollerService.cs#L206)

```csharp
cts.CancelAfter(TimeSpan.FromSeconds(60));
```

The timer fires every 2s, but a stuck query holds the `_pollerDbGate` semaphore for up to **60 seconds**, blocking all other polls.

- **Fix**: Reduce to `15s`. If a query takes >15s on a real-time dashboard, the data is already stale.

---

### 9. Duplicate Hub Mapping for the Same Class

**File**: [Program.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Program.cs#L56-L57)

```csharp
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<DashboardHub>("/hubs/livestock");
```

> [!IMPORTANT]
> Both endpoints map to the **exact same `DashboardHub` class**. SignalR creates separate hub instances per path, causing **double client count** in `_connectedClients`. If a React app connects to both, the client counter is inflated 2x.

- **Fix**: Remove `/hubs/livestock`. Update frontend to use only `/hubs/dashboard`.

---

### 10. `BaseDashboardService._keyLocks` — Unbounded SemaphoreSlim Leak

**File**: [BaseDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/Base/BaseDashboardService.cs#L18)

```csharp
private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
```

Every unique cache key creates a new `SemaphoreSlim` that is **never removed**. Over time, with different search terms and pages, this dictionary grows unboundedly.

- **Fix**: Add periodic cleanup, or use `LazyInitializer` with a bounded set of keys. For cache keys that are parameterized (search + page), consider hashing into a fixed bucket count.

---

### 11. Cache Key Includes Mutable Default Values

**Files**: Every service method in [MainDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/MainDashboard/MainDashboardService.cs)

```csharp
string cacheKey = $"LiveStockDetails_{request.UserId}_{request.SearchTerm}_{request.PageIndex}_...";
```

If the frontend sends `SortColumn=null` vs `SortColumn=""` vs `SortColumn=STORE`, these generate **different cache keys** for the same SQL query (because the service defaults `null` → `"STORE"` inside the method). This causes cache misses and redundant SQL calls.

- **Fix**: Normalize inputs _before_ building the cache key:
```csharp
string sortCol = string.IsNullOrEmpty(request.SortColumn) ? "STORE" : request.SortColumn;
string cacheKey = $"LiveStockDetails_{request.UserId}_{request.SearchTerm ?? ""}_{request.PageIndex}_{sortCol}...";
```

---

### 12. Poller Directly Writes to API Cache with Hardcoded Key

**File**: [LiveStockPollerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/LiveStockPollerService.cs#L202-L203)

```csharp
string defaultCacheKey = "LiveStockDetails_26__1_100_STORE_asc_string";
_cache.Set(defaultCacheKey, responseObj, TimeSpan.FromSeconds(30));
```

> [!WARNING]
> This hardcoded cache key **must exactly match** the key generated by `MainDashboardService.GetLiveStockDetailsAsync`. If anyone changes the key format in the service, the poller silently writes to a different key and the optimization breaks. Also, the poller sets TTL=30s but the service sets TTL=90s — inconsistent.

- **Fix**: Extract a shared `CacheKeyBuilder.LiveStock(userId, search, page, size, sort, dir, type)` method. Use it in both the poller and the service.

---

## 🟡 Medium Priority Optimizations

### 13. `commandTimeout: 120` on Every Single Query

**Files**: Every service method across all features

Every stored procedure call uses `commandTimeout: 120` (2 minutes). For a real-time dashboard, this is excessively generous. A slow query that takes 2 minutes will block the SWR refresh Task and consume a connection from the pool.

- **Fix**: Use tiered timeouts:
  - Dashboard summary endpoints: `30s`
  - Drill-down/report endpoints: `60s`
  - Cache warmer background: `120s` (acceptable)

---

### 14. `Newtonsoft.Json` Package Unused

**File**: [VS Mart Backend.csproj](../VS%20Mart%20Backend/VS%20Mart%20Backend/VS%20Mart%20Backend.csproj#L14)

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

Only [HUDiscrepancyService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/HUDiscrepancy/HUDiscrepancyService.cs#L6) imports `Newtonsoft.Json` but **never uses it in code**. ASP.NET Core uses `System.Text.Json` by default.

- **Fix**: Remove the unused `using Newtonsoft.Json;` import and the NuGet package reference.

---

### 15. Silent Exception Swallowing in Services

**Files**: [MainDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/MainDashboard/MainDashboardService.cs#L484-L487), [DcDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/DcDashboard/DcDashboardService.cs#L68-L70), [SaleDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/SaleDashboard/SaleDashboardService.cs)

```csharp
catch (Exception)
{
    return new VendorHUDiscrepancyResponse(); // ← Silent empty response
}
```

Multiple services catch all exceptions and return empty responses with no logging. The frontend shows an empty table, the user assumes "no data", but the actual cause could be a SQL timeout, a misconfigured SP, or a connection pool exhaustion.

- **Fix**: Log the exception with `_logger.LogError(ex, ...)` before returning the fallback.

---

### 16. `Console.WriteLine` + `Console.ForegroundColor` for Error Logging

**File**: [CycleCountReportService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/CycleCountReport/CycleCountReportService.cs#L66) and [L122-124](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/CycleCountReport/CycleCountReportService.cs#L122-L124)

```csharp
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine($"[CycleCountDetails ERROR]: {ex.Message}");
Console.ResetColor();
```

> [!IMPORTANT]
> `Console.WriteLine` doesn't go through the structured logging pipeline, can't be filtered, and won't appear in Application Insights / Serilog / log aggregators. Also, `Console.ForegroundColor` is **not thread-safe** — two concurrent requests can corrupt each other's console colors.

- **Fix**: Replace with `_logger.LogError(ex, "...")`. Inject `ILogger<CycleCountReportService>` into the constructor.

---

### 17. `LiveStockHub` Empty Subclass — Confusing Dead Code

**File**: [DashboardHub.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/Hubs/DashboardHub.cs#L73-L75)

```csharp
public class LiveStockHub : DashboardHub { }
```

This class exists as a "backward-compatible alias" but is never referenced in `Program.cs` or anywhere else. It's purely confusing.

- **Fix**: Delete it.

---

### 18. `DashboardHub.ConnectedClientsCount` Can Go Negative

**File**: [DashboardHub.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/Hubs/DashboardHub.cs#L12-L24)

The `Math.Max(0, Volatile.Read(ref _connectedClients))` on the getter prevents returning a negative number, but the underlying counter can still underflow if `OnDisconnectedAsync` fires for connections that were never incremented (e.g., during app restart).

- **Fix**: Use `Interlocked.CompareExchange` with a floor of 0 in the decrement path.

---

### 19. CacheWarmerService — Sequential Warming is Slow

**File**: [CacheWarmerService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Services/CacheWarmerService.cs#L46-L76)

Each cache warm call runs **sequentially** with a 1-second delay between them. With 11 endpoints, one full warm cycle takes **~22 seconds** (11 queries + 11 × 1s delay). This means after startup, the cache takes 22s to be fully warm.

- **Fix**: Group independent queries into parallel batches:
```csharp
await Task.WhenAll(
    liveStockService.GetLiveStockDetailsAsync(liveStockRequest),
    liveStockService.GetCycleCountDashboardAsync(cycleCountRequest),
    liveStockService.GetVendorHUDiscrepancyAsync(vendorHuRequest)
);
```

---

### 20. `MasterService` — No Caching, No BaseDashboardService

**File**: [MasterService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Master/MasterService.cs)

`MasterService` does **not** extend `BaseDashboardService` and has no caching. Repeated calls to `SP_Master` with the same `Status` hit SQL every time.

- **Fix**: If master data is read-only (store lists, user lists), cache it using `IMemoryCache`.

---

### 21. `DcDashboardService` Calls `SP_NEW_DASHBOARD` (Casing Inconsistency)

**File**: [DcDashboardService.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Features/Dashboard/DcDashboard/DcDashboardService.cs#L56)

```csharp
await connection.QueryAsync<dynamic>("SP_NEW_DASHBOARD", ...);
```

Every other service calls `SP_New_Dashboard` or `SP_NEW_REPORT`. This file calls `SP_NEW_DASHBOARD` (all caps). If SQL Server collation is case-sensitive, this fails silently.

- **Fix**: Standardize stored procedure name constants across all services.

---

## 🟢 Low Priority

### 22. No Health Check Endpoint

The app has no `/health` or `/healthz` endpoint. Without this, load balancers (IIS ARR, nginx, Azure App Service) can't detect if the app is healthy.

- **Fix**: Add `app.MapHealthChecks("/health")` with a SQL connection check.

---

### 23. No Response Compression

All API responses (often large JSON arrays of 100 rows) are sent uncompressed.

- **Fix**: Add `builder.Services.AddResponseCompression(...)` with Gzip/Brotli.

---

### 24. No Request Rate Limiting

Any client can spam all endpoints with infinite requests. Combined with no auth, this is a potential overload vector.

- **Fix**: Add `builder.Services.AddRateLimiter(...)` (built into .NET 8).

---

### 25. Swagger Enabled in Production

**File**: [Program.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Program.cs#L48-L49)

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

These are outside any `if (app.Environment.IsDevelopment())` guard. Swagger UI is accessible in production, exposing your full API surface.

- **Fix**: Wrap in development check:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

### 26. `CORS AllowAll` with `SetIsOriginAllowed(_ => true)`

**File**: [Program.cs](../VS%20Mart%20Backend/VS%20Mart%20Backend/Program.cs#L16-L22)

Allowing any origin with credentials is the most permissive CORS configuration possible. Fine for development, but should be tightened with an allowed origin list for production.

---

### 27. `Connection Lifetime=300` in Connection String

**File**: [appsettings.json](../VS%20Mart%20Backend/VS%20Mart%20Backend/appsettings.json#L11)

`Connection Lifetime=300` forces connections to be recycled every 5 minutes. Combined with `Min Pool Size=20`, this creates constant connection churn. For a local SQL Server, this is unnecessary overhead.

---

### 28. `RollForward=Major` in `.csproj`

**File**: [VS Mart Backend.csproj](../VS%20Mart%20Backend/VS%20Mart%20Backend/VS%20Mart%20Backend.csproj#L5)

Allows the app to silently run on .NET 9+ even though it targets `net8.0`. Better to be explicit to avoid unexpected runtime behaviors.

---

### 29. `test_api.json` Committed to Repository

**File**: [test_api.json](../VS%20Mart%20Backend/VS%20Mart%20Backend/test_api.json) (14KB)

This file appears to be local test/debug payload data that should not be tracked in production git history.

---

### 30. `appsettings.json` Contains Production Credentials in Git

**File**: [appsettings.json](../VS%20Mart%20Backend/VS%20Mart%20Backend/appsettings.json#L11)

The `sa` password is committed to Git in plaintext.

- **Fix**: Use `dotnet user-secrets` for local dev, environment variables or Azure Key Vault for staging/production.

---

## ✅ What's Already Done Well

| Area | Assessment |
|---|---|
| **Dapper over EF** | ✅ Correct choice for SP-heavy, read-optimized workloads |
| **SWR Caching Pattern** | ✅ Well-implemented stale-while-revalidate with stampede protection |
| **SignalR Delta Patching** | ✅ Excellent design — only sends changes, not full refreshes |
| **Hub Occupancy Check** | ✅ Pollers correctly skip when no clients are connected |
| **Feature-Based Folder Structure** | ✅ Clean separation under `Features/` |
| **Dapper `dynamic` → `Dictionary` pattern** | ✅ CS8620-safe with `StringComparer.OrdinalIgnoreCase` |
| **Parameterized SP calls** | ✅ No SQL injection risk |
| **PeriodicTimer** | ✅ Modern, GC-friendly replacement for `Task.Delay` loops |
| **SemaphoreSlim DB Gate** | ✅ Prevents pollers from opening unbounded connections |

---

## 📊 Priority & Effort Matrix

| # | Issue | Effort | Impact | Priority |
|---|---|---|---|:---:|
| 1 | Delete SSE dead code | 5 min | High | 🔴 |
| 2 | Delete sync `Login()` | 2 min | High | 🔴 |
| 3 | JWT Authentication & RBAC | 2–3 days | Critical | 🔴 |
| 4 | Move User ID 26 to config | 10 min | Medium | 🔴 |
| 5 | Replace `File.AppendAllText` | 15 min | High | 🔴 |
| 6 | Single connection for CycleCount | 30 min | High | 🟠 |
| 7 | Fix O(n²) joins | 20 min | Medium | 🟠 |
| 8 | Reduce poller timeout | 5 min | Medium | 🟠 |
| 9 | Remove duplicate hub mapping | 5 min | Medium | 🟠 |
| 10 | Fix `_keyLocks` memory leak | 30 min | Medium | 🟠 |
| 11 | Normalize cache keys | 30 min | Medium | 🟡 |
| 12 | Shared cache key builder | 1 hr | Medium | 🟡 |
| 13 | Tiered command timeouts | 15 min | Low | 🟡 |
| 14 | Remove `Newtonsoft.Json` | 5 min | Low | 🟡 |
| 15 | Add exception logging | 30 min | Medium | 🟡 |
| 16 | Replace `Console.WriteLine` | 10 min | Low | 🟡 |
| 17 | Delete `LiveStockHub` alias | 2 min | Low | 🟡 |
| 18 | Fix client counter underflow | 10 min | Low | 🟡 |
| 19 | Parallel cache warming | 30 min | Medium | 🟡 |
| 20 | Cache master data | 20 min | Low | 🟡 |
| 21 | Standardize SP name casing | 15 min | Low | 🟡 |
| 22 | Add health check | 10 min | Medium | 🟢 |
| 23 | Response compression | 10 min | Medium | 🟢 |
| 24 | Rate limiting | 30 min | Medium | 🟢 |
| 25 | Guard Swagger for dev only | 5 min | Low | 🟢 |
| 26 | Tighten CORS | 10 min | Low | 🟢 |

---

> [!TIP]
> **Recommended Quick Wins (< 30 minutes total)**:
> Items **1, 2, 4, 8, 9, 14, 17, 25** can all be completed in a single commit with immediate performance and hygiene gains!
