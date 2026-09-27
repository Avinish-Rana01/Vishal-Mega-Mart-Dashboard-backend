# Document 03: Modern .NET 8/9 Backend Architecture
## Enterprise Web API Blueprint: Vertical Slices, CQRS, and Type Safety

> **Purpose:** Replace loosely typed controllers and scattered services with a high-performance, maintainable Vertical Slice Architecture in .NET 8/9.

---

## 1. Architectural Style: Vertical Slice Architecture (VSA)

In a traditional "Clean Architecture" or 3-tier architecture, code is organized by technical layer (`Controllers`, `Services`, `Repositories`, `Models`). Adding a single report requires editing 5 or 6 separate folders.

In **Vertical Slice Architecture**, code is organized around **features and business capabilities**:

```
Features/
├── Auth/
├── Dashboard/
│   ├── MainDashboard/
│   └── LiveStockTelemetry/
└── Reports/
    ├── VoidDetails/
    │   ├── GetVoidDetailsEndpoint.cs       # API route & HTTP contract
    │   ├── GetVoidDetailsQuery.cs          # Strongly typed request DTO
    │   ├── GetVoidDetailsResponse.cs       # Strongly typed response DTO
    │   ├── GetVoidDetailsValidator.cs      # FluentValidation rules
    │   └── GetVoidDetailsHandler.cs        # Dapper data access & SP execution
    ├── StoreGrc/
    │   ├── GetStoreGrcEndpoint.cs
    │   └── ...
    └── CycleCount/
        ├── GetCycleCountEndpoint.cs
        └── ...
```

### Why Vertical Slices Excel in Enterprise Dashboards:
1. **Zero Coupling Between Reports:** Changes to `VoidDetails` cannot accidentally break `StoreGrc` or `CycleCount`.
2. **High Developer Velocity:** Everything needed to understand, modify, test, or debug a report is in one single directory.
3. **Optimized Queries:** Each feature writes the exact, optimized query it needs without forcing a generic repository pattern.

---

## 2. Universal Data Transfer Objects (DTOs)

### 2.1 The Standard Paged Result Model
Every report endpoint in the application returns data conforming to a standardized, generic response envelope:

```csharp
namespace VMM.Retail.Shared.Models;

public record PagedResult<TItem, TSummary>(
    IReadOnlyList<TItem> Items,
    TSummary Summary,
    int PageIndex,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}
```

---

### 2.2 Strongly Typed Sort Enums (Eliminating String Fragility)
Instead of accepting arbitrary string parameters (`string? sortColumn`), define a strictly typed enum for every report:

```csharp
namespace VMM.Retail.Features.Reports.VoidDetails;

public enum VoidSortField
{
    TransactionDate,
    VoidQty,
    EncodedQty,
    PendingQty,
    StoreCode,
    StoreName
}

public enum SortDirection
{
    Asc,
    Desc
}

public record GetVoidDetailsQuery(
    string? StoreCode,
    DateTime FromDate,
    DateTime ToDate,
    string? SearchTerm,
    VoidSortField SortBy = VoidSortField.TransactionDate,
    SortDirection Direction = SortDirection.Desc,
    int PageIndex = 1,
    int PageSize = 10
);
```

---

## 3. Pre-Database Request Validation (FluentValidation)

Never let invalid inputs reach the database. Use **FluentValidation** to enforce business constraints automatically:

```csharp
using FluentValidation;

namespace VMM.Retail.Features.Reports.VoidDetails;

public class GetVoidDetailsValidator : AbstractValidator<GetVoidDetailsQuery>
{
    public GetVoidDetailsValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageIndex must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 500)
            .WithMessage("PageSize must be between 1 and 500.");

        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("FromDate is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate cannot be earlier than FromDate.");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 93)
            .WithMessage("Date range cannot exceed 3 months for performance.");
    }
}
```

---

## 4. High-Performance Feature Handler (Dapper + SP Execution)

Here is the complete implementation of a feature handler that executes the dedicated stored procedure with zero dynamic string parsing:

```csharp
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using VMM.Retail.Shared.Models;

namespace VMM.Retail.Features.Reports.VoidDetails;

public interface IGetVoidDetailsHandler
{
    Task<PagedResult<VoidItemDto, VoidSummaryDto>> HandleAsync(GetVoidDetailsQuery query, CancellationToken ct);
}

public class GetVoidDetailsHandler : IGetVoidDetailsHandler
{
    private readonly string _connectionString;

    public GetVoidDetailsHandler(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection not configured.");
    }

    public async Task<PagedResult<VoidItemDto, VoidSummaryDto>> HandleAsync(GetVoidDetailsQuery query, CancellationToken ct)
    {
        using var connection = new SqlConnection(_connectionString);

        // Map Enum to Database Sort Column safely
        string dbSortColumn = query.SortBy switch
        {
            VoidSortField.VoidQty         => "void_qty",
            VoidSortField.EncodedQty      => "encoded_qty",
            VoidSortField.PendingQty      => "pending_qty",
            VoidSortField.StoreCode       => "store_code",
            VoidSortField.StoreName       => "store_name",
            _                             => "transaction_date"
        };

        var p = new DynamicParameters();
        p.Add("@StoreCode", query.StoreCode ?? string.Empty);
        p.Add("@FromDate", query.FromDate.Date);
        p.Add("@ToDate", query.ToDate.Date);
        p.Add("@SearchTerm", query.SearchTerm);
        p.Add("@SortColumn", dbSortColumn);
        p.Add("@SortDirection", query.Direction.ToString().ToUpperInvariant());
        p.Add("@PageIndex", query.PageIndex);
        p.Add("@PageSize", query.PageSize);

        // Output parameters for aggregates
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@TotalVoidQty", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@TotalEncodeQty", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@TotalDiffQty", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await connection.QueryAsync<VoidItemDto>(
            new CommandDefinition(
                "dbo.usp_Report_VoidDetails",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        var summary = new VoidSummaryDto(
            TotalVoidQty: p.Get<int?>("@TotalVoidQty") ?? 0,
            TotalEncodedQty: p.Get<int?>("@TotalEncodeQty") ?? 0,
            TotalPendingQty: p.Get<int?>("@TotalDiffQty") ?? 0
        );

        int totalCount = p.Get<int?>("@TotalCount") ?? 0;

        return new PagedResult<VoidItemDto, VoidSummaryDto>(
            Items: items.ToList(),
            Summary: summary,
            PageIndex: query.PageIndex,
            PageSize: query.PageSize,
            TotalCount: totalCount
        );
    }
}
```

---

## 5. Modern Endpoint Definition (.NET 8 Minimal API)

Minimal APIs provide better throughput and reduced memory allocations compared to heavy MVC controllers:

```csharp
namespace VMM.Retail.Features.Reports.VoidDetails;

public static class VoidDetailsEndpoints
{
    public static void MapVoidDetailsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/void-details", async (
            [AsParameters] GetVoidDetailsQuery query,
            IGetVoidDetailsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetVoidDetailsReport")
        .WithSummary("Retrieves paginated Void Details report with summary totals")
        .Produces<PagedResult<VoidItemDto, VoidSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("ReportRateLimitPolicy");
    }
}
```

---

## 6. Real-Time SignalR Hub Architecture & Resilience

To maintain live inventory broadcasting without freezing client sockets:

1. **Backpressure Buffer:** Use `Channel<T>` to queue incoming RFID telemetry without blocking HTTP API requests.
2. **Heartbeat & Circuit Breaker:** Configure standard keep-alive pings (15 seconds) and automatic client reconnect intervals:
   ```csharp
   builder.Services.AddSignalR(options =>
   {
       options.EnableDetailedErrors = builder.Environment.IsDevelopment();
       options.KeepAliveInterval = TimeSpan.FromSeconds(15);
       options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
       options.MaximumReceiveMessageSize = 64 * 1024; // 64KB max packet
   });
   ```
3. **Broadcast Diffs Only:** Instead of sending the full 10,000-row table on every update, send only changed store rows using a JSON patch envelope.
