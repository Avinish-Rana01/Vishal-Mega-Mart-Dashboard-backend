using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Base
{
    public class UserIdValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserIdValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Only enforce on /api/ routes
            if (path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase))
            {
                // Exempt public endpoints: login, change-password, cache/system telemetry, swagger, SignalR hubs
                if (path.StartsWith("/api/Auth/login", System.StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/Auth/change-password", System.StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/system/", System.StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/hubs/", System.StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                int resolvedUserId = 0;

                // 1. Check Query String (?userId=... or ?UserId=... or ?user_id=...)
                if (context.Request.Query.TryGetValue("userId", out var qUserId) ||
                    context.Request.Query.TryGetValue("UserId", out qUserId) ||
                    context.Request.Query.TryGetValue("user_id", out qUserId))
                {
                    int.TryParse(qUserId.ToString(), out resolvedUserId);
                }

                // 2. Check HTTP Header (X-User-Id)
                if (resolvedUserId <= 0)
                {
                    if (context.Request.Headers.TryGetValue("X-User-Id", out var hUserId) ||
                        context.Request.Headers.TryGetValue("x-user-id", out hUserId) ||
                        context.Request.Headers.TryGetValue("UserId", out hUserId))
                    {
                        int.TryParse(hUserId.ToString(), out resolvedUserId);
                    }
                }

                // 3. Check JSON Body (for POST/PUT requests where userId is passed in payload)
                if (resolvedUserId <= 0 && context.Request.HasJsonContentType() && (context.Request.ContentLength ?? 0) > 0)
                {
                    context.Request.EnableBuffering();
                    using var reader = new System.IO.StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                if (doc.RootElement.TryGetProperty("userId", out var prop) ||
                                    doc.RootElement.TryGetProperty("UserId", out prop) ||
                                    doc.RootElement.TryGetProperty("user_id", out prop))
                                {
                                    if (prop.ValueKind == JsonValueKind.Number)
                                        resolvedUserId = prop.GetInt32();
                                    else if (prop.ValueKind == JsonValueKind.String)
                                        int.TryParse(prop.GetString(), out resolvedUserId);
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 4. Reject if missing or <= 0
                if (resolvedUserId <= 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new
                    {
                        statusCode = 400,
                        error = "Bad Request",
                        message = "userId is mandatory for this request. Please provide a valid userId query parameter (?userId=...) or X-User-Id header."
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                // Attach resolved userId to HttpContext items for easy downstream consumption
                context.Items["UserId"] = resolvedUserId;
            }

            await _next(context);
        }
    }
}
