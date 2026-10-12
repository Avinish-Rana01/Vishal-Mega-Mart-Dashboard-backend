using Microsoft.AspNetCore.Http;

namespace VS_Mart_Backend.Features.Base
{
    public static class UserIdExtensions
    {
        public static int GetResolvedUserId(this HttpContext? context, int? explicitUserId = null)
        {
            if (explicitUserId.HasValue && explicitUserId.Value > 0)
                return explicitUserId.Value;

            if (context != null && context.Items.TryGetValue("UserId", out var ctxId) && ctxId is int id && id > 0)
                return id;

            return 0;
        }

        public static int GetResolvedUserId(this HttpContext? context, string? explicitUserId)
        {
            if (!string.IsNullOrWhiteSpace(explicitUserId) && int.TryParse(explicitUserId, out var id) && id > 0)
                return id;

            if (context != null && context.Items.TryGetValue("UserId", out var ctxId) && ctxId is int idVal && idVal > 0)
                return idVal;

            return 0;
        }
    }
}
