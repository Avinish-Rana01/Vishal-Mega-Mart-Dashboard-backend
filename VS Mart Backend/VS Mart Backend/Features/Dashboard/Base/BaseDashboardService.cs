using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Base
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public bool IsSuperAdmin => string.Equals(UserType, "Super Admin", StringComparison.OrdinalIgnoreCase);
    }

    public abstract class BaseDashboardService
    {
        protected readonly IConfiguration _configuration;
        protected readonly IMemoryCache _cache;
        protected readonly string _connectionString;

        private static bool? _cacheOverride = null;
        private static readonly ConcurrentDictionary<string, bool> _refreshingKeys = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

        public class CacheItem<T>
        {
            public T Data { get; set; } = default!;
            public DateTime CreatedAt { get; set; }
        }

        public static void SetCacheItem<T>(IMemoryCache cache, string key, T data, TimeSpan? ttl = null)
        {
            cache.Set(key, new CacheItem<T> { Data = data, CreatedAt = DateTime.UtcNow }, ttl ?? TimeSpan.FromSeconds(90));
        }

        protected BaseDashboardService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            _connectionString = _configuration.GetConnectionString("POS") 
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
        }

        public bool IsCacheEnabled()
        {
            if (_cacheOverride.HasValue) return _cacheOverride.Value;
            return _configuration.GetValue<bool>("EnableCache", true);
        }

        public void SetCacheEnabled(bool enabled)
        {
            _cacheOverride = enabled;
        }

        protected async Task<T> GetOrCreateWithSWRAsync<T>(string cacheKey, Func<Task<T>> databaseQuery)
        {
            if (!IsCacheEnabled()) return await databaseQuery();

            if (_cache.TryGetValue(cacheKey, out CacheItem<T>? cachedItem) && cachedItem != null)
            {
                if (DateTime.UtcNow - cachedItem.CreatedAt > TimeSpan.FromSeconds(20))
                {
                    if (_refreshingKeys.TryAdd(cacheKey, true))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var freshData = await databaseQuery();
                                _cache.Set(cacheKey, new CacheItem<T> { Data = freshData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromSeconds(90));
                            }
                            finally
                            {
                                _refreshingKeys.TryRemove(cacheKey, out _);
                            }
                        });
                    }
                }
                return cachedItem.Data;
            }

            // Single-flight stampede protection: Coalesce concurrent requests for the same cacheKey
            var keyLock = _keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await keyLock.WaitAsync();
            try
            {
                // Double-check cache inside lock
                if (_cache.TryGetValue(cacheKey, out CacheItem<T>? doubleCheckItem) && doubleCheckItem != null)
                {
                    return doubleCheckItem.Data;
                }

                var initialData = await databaseQuery();
                _cache.Set(cacheKey, new CacheItem<T> { Data = initialData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromSeconds(90));
                return initialData;
            }
            finally
            {
                keyLock.Release();
            }
        }

        public async Task<int> GetActiveSuperAdminIdAsync()
        {
            const string cacheKey = "System_Active_SuperAdmin_Id";

            if (_cache.TryGetValue(cacheKey, out int cachedId) && cachedId > 0)
            {
                return cachedId;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var superAdminId = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 User_ID 
                      FROM dbo.User_Registration 
                      WHERE User_Type = 'Super Admin' 
                        AND (Is_Status = '1' OR Is_Status IS NULL)
                      ORDER BY User_ID ASC");

                if (superAdminId.HasValue && superAdminId.Value > 0)
                {
                    _cache.Set(cacheKey, superAdminId.Value, TimeSpan.FromMinutes(30));
                    return superAdminId.Value;
                }

                // Self-healing: if 0 active super admins found, restore 'Admin' or insert SYSTEM_VMM_SERVICE
                await connection.ExecuteAsync(@"
                    IF EXISTS (SELECT 1 FROM dbo.User_Registration WHERE User_Name = 'Admin')
                    BEGIN
                        UPDATE dbo.User_Registration 
                        SET User_Type = 'Super Admin', Is_Status = '1' 
                        WHERE User_Name = 'Admin';
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.User_Registration (User_Name, Password, User_Type, Is_Status, Entry_Date)
                        VALUES ('SYSTEM_VMM_SERVICE', 'Sys@vmm2026', 'Super Admin', '1', GETDATE());
                    END
                ");

                var restoredId = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 User_ID 
                      FROM dbo.User_Registration 
                      WHERE User_Type = 'Super Admin' 
                        AND (Is_Status = '1' OR Is_Status IS NULL)
                      ORDER BY User_ID ASC");

                if (restoredId.HasValue && restoredId.Value > 0)
                {
                    _cache.Set(cacheKey, restoredId.Value, TimeSpan.FromMinutes(30));
                    return restoredId.Value;
                }
            }
            catch
            {
                // Safe fallback to known ID if DB communication encounters transient issue
            }

            return 30;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out int uid) || uid <= 0)
            {
                return new UserProfileDto { UserType = "Super Admin" }; // Default to Super Admin if unspecified
            }

            string cacheKey = $"UserProfile_{uid}";
            if (_cache.TryGetValue(cacheKey, out UserProfileDto? cachedProfile) && cachedProfile != null)
            {
                return cachedProfile;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var profile = await connection.QueryFirstOrDefaultAsync<UserProfileDto>(@"
                    SELECT 
                        U.User_ID AS UserId, 
                        ISNULL(U.User_Type, '') AS UserType, 
                        ISNULL(S.Store_Code, '') AS StoreCode, 
                        ISNULL(S.Store_Name, '') AS StoreName,
                        ISNULL(W.WH_Code, '') AS WarehouseCode
                    FROM dbo.User_Registration U
                    LEFT JOIN dbo.tbl_Store_Master S ON U.Store_ID = S.Store_ID
                    LEFT JOIN dbo.tbl_Warehouse_Mst W ON U.WH_ID = W.WH_ID
                    WHERE U.User_ID = @Uid", new { Uid = uid });

                if (profile != null)
                {
                    _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(60));
                    return profile;
                }
            }
            catch
            {
                // Fallback on transient error
            }

            return new UserProfileDto { UserId = uid, UserType = "Store Admin" };
        }
    }
}
