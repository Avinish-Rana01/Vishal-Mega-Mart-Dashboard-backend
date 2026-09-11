using VS_Mart_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Add Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for frontend access (supporting WebSockets and SignalR credentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();

// Register Dependency Injection Services
builder.Services.AddSingleton<SseNotifierService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.MainDashboard.IMainDashboardService, VS_Mart_Backend.Features.MainDashboard.MainDashboardService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.LiveStockReport.ILiveStockReportService, VS_Mart_Backend.Features.LiveStockReport.LiveStockReportService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.StoreGrcReport.IStoreGrcReportService, VS_Mart_Backend.Features.StoreGrcReport.StoreGrcReportService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.CycleCountReport.ICycleCountReportService, VS_Mart_Backend.Features.CycleCountReport.CycleCountReportService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.SaleDashboard.ISaleDashboardService, VS_Mart_Backend.Features.SaleDashboard.SaleDashboardService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.HUDiscrepancy.IHUDiscrepancyService, VS_Mart_Backend.Features.HUDiscrepancy.HUDiscrepancyService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.ReturnDashboard.IReturnDashboardService, VS_Mart_Backend.Features.ReturnDashboard.ReturnDashboardService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.DcDashboard.IDcDashboardService, VS_Mart_Backend.Features.DcDashboard.DcDashboardService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.VoidDashboard.IVoidDashboardService, VS_Mart_Backend.Features.VoidDashboard.VoidDashboardService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.Auth.IAuthService, VS_Mart_Backend.Features.Auth.AuthService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.SystemUtility.ISystemUtilityService, VS_Mart_Backend.Features.SystemUtility.SystemUtilityService>();
builder.Services.AddScoped<VS_Mart_Backend.Features.Master.IMasterService, VS_Mart_Backend.Features.Master.MasterService>();
// builder.Services.AddHostedService<VS_Mart_Backend.Services.CacheWarmerService>(); // Background worker
// builder.Services.AddHostedService<VS_Mart_Backend.Services.LiveStockPollerService>(); // 2-second background poller for real-time Live Stock
builder.Services.AddHostedService<VS_Mart_Backend.Services.DashboardSectionsPollerService>(); // Background poller for Cycle Count, Store Validation, DC Encoding, Tag Management, Vendor Discrepancy

var app = builder.Build();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
app.MapHub<VS_Mart_Backend.Features.Dashboard.Hubs.DashboardHub>("/hubs/dashboard");
app.MapHub<VS_Mart_Backend.Features.Dashboard.Hubs.DashboardHub>("/hubs/livestock");

app.Run();
