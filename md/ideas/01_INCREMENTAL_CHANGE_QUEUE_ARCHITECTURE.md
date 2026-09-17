# Architecture Proposal: Incremental Change Queue (Event-Driven Dashboard Updates)

## 1. Problem Statement
The current dashboard architecture periodically executes massive stored procedures (`SP_New_Dashboard` and `SP_NEW_REPORT`) inside background workers (`LiveStockPollerService`, `DashboardSectionsPollerService`, `CacheWarmerService`).
- These procedures perform heavy full-table scans, joins, and aggregations across hundreds of thousands of RFID records.
- Concurrently executing multiple instances of these stored procedures overwhelms SQL Server CPU, bloats `tempdb`, causes parallel thread lock waits (`CXSYNC_PORT`), and exhausts the ADO.NET connection pool.
- Lightweight queries—such as user login and navigation—get queued behind these background scans, causing timeout aborts (`login (canceled)`).

---

## 2. Proposed Architecture: Incremental Change Queue

Instead of re-reading historical data to recount totals, the system moves to an **Event-Driven Write-Side Increment** pattern.

```
┌────────────────────────────────────────────────────────┐
│                   WRITE PATH (Store / HHT)             │
│  Tag Scanned / HU Validated / Discrepancy Found        │
└──────────────────────────┬─────────────────────────────┘
                           │ INSERT / UPDATE
                           ▼
┌────────────────────────────────────────────────────────┐
│                   SQL SERVER TABLES                    │
│   Triggers write 1 tiny delta row (< 0.1 ms overhead)   │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│             dbo.DashboardChangeQueue Table             │
│  (Contains only unprocessed deltas: 0 - 5 rows)        │
└──────────────────────────┬─────────────────────────────┘
                           │ Fast Non-blocking Fetch (< 1 ms)
                           ▼
┌────────────────────────────────────────────────────────┐
│                 .NET 8 BACKEND WORKER                  │
│  1. Deletes & consumes delta rows (OUTPUT DELETED.*)   │
│  2. Applies delta to In-Memory Snapshot (IMemoryCache) │
│  3. Broadcasts delta patch to SignalR Dashboard Hub    │
└──────────────────────────┬─────────────────────────────┘
                           │ WebSocket Push (Instant)
                           ▼
┌────────────────────────────────────────────────────────┐
│               FRONTEND REACT APPLICATION               │
│  Receives patch and animates counter update (Flash)    │
└────────────────────────────────────────────────────────┘
```

---

## 3. Database Schema

### Queue Table Definition
```sql
CREATE TABLE dbo.DashboardChangeQueue (
    QueueId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Section VARCHAR(50) NOT NULL,       -- 'StoreValidation', 'CycleCount', 'LiveStock', etc.
    StoreCode VARCHAR(20) NOT NULL,     -- 'HD55'
    Metric VARCHAR(50) NOT NULL,        -- 'HU_VALIDATED_QTY', 'SCANNED_QTY', etc.
    DeltaQty INT NOT NULL,              -- +1, -1, +5
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Index for instant consumption and cleanup
CREATE NONCLUSTERED INDEX IX_DashboardChangeQueue_Section 
ON dbo.DashboardChangeQueue (Section, QueueId);
```

### Example Trigger on Operational Table
```sql
CREATE OR ALTER TRIGGER trg_HU_Validation_Insert
ON dbo.HU_VALIDATION_DETAILS
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DashboardChangeQueue (Section, StoreCode, Metric, DeltaQty)
    SELECT 
        'StoreValidation',
        i.Store_Code,
        'HU_VALIDATED_QTY',
        COUNT(1)
    FROM inserted i
    GROUP BY i.Store_Code;
END;
```

---

## 4. Backend Processing Logic (C# .NET 8)

### Atomic Dequeue & In-Memory Application
```csharp
public async Task ProcessPendingDeltasAsync(CancellationToken ct)
{
    const string dequeueSql = @"
        DELETE FROM dbo.DashboardChangeQueue 
        OUTPUT DELETED.QueueId, DELETED.Section, DELETED.StoreCode, DELETED.Metric, DELETED.DeltaQty
        WHERE QueueId IN (
            SELECT TOP 50 QueueId 
            FROM dbo.DashboardChangeQueue WITH (READPAST, ROWLOCK)
            ORDER BY QueueId ASC
        );";

    using var connection = new SqlConnection(_connectionString);
    var deltas = (await connection.QueryAsync<ChangeDelta>(new CommandDefinition(dequeueSql, cancellationToken: ct))).ToList();

    if (!deltas.Any()) return;

    foreach (var delta in deltas)
    {
        // 1. Update in-memory snapshot in IMemoryCache
        _cacheService.ApplyDelta(delta);

        // 2. Broadcast delta to all connected clients over SignalR
        await _hubContext.Clients.All.SendAsync("ReceiveSectionPatch", new {
            section = delta.Section,
            storeCode = delta.StoreCode,
            metric = delta.Metric,
            delta = delta.DeltaQty
        }, ct);
    }
}
```

---

## 5. Architectural Benefits

| Metric | Current Polling Approach | Incremental Queue Approach |
| :--- | :--- | :--- |
| **SQL Query Complexity** | 10,000 lines of complex joins/aggregations | 1-line `INSERT` + 1-line `DELETE` |
| **Execution Duration** | 3,000 ms – 10,000 ms per iteration | < 1 ms per iteration |
| **SQL Server CPU Load** | 70% – 100% (Causes locks) | < 2% (Near zero) |
| **Database Locks** | Shared read locks & `CXSYNC_PORT` waits | Microsecond row-locks only |
| **Login & API Latency** | Intermittently blocked (up to 30s) | Instant (< 20 ms) |
| **Update Latency** | 5 – 10 seconds | Under 50 milliseconds |

---

## 6. Migration & Rollback Strategy
1. **Initial Baseline:** On application boot, the backend runs `SP_New_Dashboard` once to establish baseline metrics in memory.
2. **Real-Time Increments:** From that point forward, all changes are processed via `DashboardChangeQueue`.
3. **Hourly Reconciliation (Safety Net):** An hourly background task can perform a non-blocking background reconciliation to ensure zero drift over long periods.
