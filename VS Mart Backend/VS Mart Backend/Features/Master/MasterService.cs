using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS_Mart_Backend.Features.Base;
using VS_Mart_Backend.Services;

namespace VS_Mart_Backend.Features.Master
{
    public class MasterService : IMasterService
    {
        private readonly string _connectionString;
        private readonly ILogger<MasterService> _logger;
        private readonly IMemoryCache _cache;

        public MasterService(IConfiguration configuration, ILogger<MasterService> logger, IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("POS")
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
            _logger = logger;
            _cache = cache;
        }

        public async Task<MasterResponse> ExecuteMasterAsync(MasterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return new MasterResponse
                {
                    Success = false,
                    Message = "Status parameter is required."
                };
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);

                // 1. Status translation mapping between frontend requests and new DB SP_Master
                string status = request.Status?.Trim() ?? string.Empty;
                string normalizedStatus = status.ToLowerInvariant();

                if (normalizedStatus == "sp_bind_floormaster" || normalizedStatus == "sp_bind_floor_master")
                    status = "SP_Bind_Store_Floor_Master";
                else if (normalizedStatus == "insert_tbl_store_floor_mst")
                    status = "Insert_tbl_Store_Floor_Master";
                else if (normalizedStatus == "update_tbl_store_floor_mst")
                    status = "Update_tbl_Store_Floor_Master";
                else if (normalizedStatus == "delete_tbl_store_floor_mst")
                    status = "Delete_tbl_Store_Floor_Master";
                else if (normalizedStatus == "sp_bind_usermaster")
                    status = "SP_Bind_User_Master";

                // 2. Resolve IDs from string names if frontend passed text values
                int stateId = request.State_ID;
                if (stateId <= 0 && !string.IsNullOrWhiteSpace(request.State))
                {
                    if (int.TryParse(request.State, out int parsedId)) stateId = parsedId;
                    else
                    {
                        var sId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 State_ID FROM dbo.tbl_State_Mst WHERE State_Name = @Name", new { Name = request.State.Trim() });
                        stateId = sId ?? 0;
                    }
                }

                int cityId = request.City_ID;
                if (cityId <= 0 && !string.IsNullOrWhiteSpace(request.City))
                {
                    if (int.TryParse(request.City, out int parsedId)) cityId = parsedId;
                    else
                    {
                        var cId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 City_ID FROM dbo.tbl_City_Mst WHERE City_Name = @Name", new { Name = request.City.Trim() });
                        cityId = cId ?? 0;
                    }
                }

                int smId = request.SM_ID;
                if (smId <= 0 && !string.IsNullOrWhiteSpace(request.Store_Manager))
                {
                    if (int.TryParse(request.Store_Manager, out int parsedId)) smId = parsedId;
                    else
                    {
                        var uId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 User_ID FROM dbo.User_Registration WHERE User_Name = @Name", new { Name = request.Store_Manager.Trim() });
                        smId = uId ?? 0;
                    }
                }

                int amId = request.AM_ID;
                if (amId <= 0 && !string.IsNullOrWhiteSpace(request.Area_Manager))
                {
                    if (int.TryParse(request.Area_Manager, out int parsedId)) amId = parsedId;
                    else
                    {
                        var uId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 User_ID FROM dbo.User_Registration WHERE User_Name = @Name", new { Name = request.Area_Manager.Trim() });
                        amId = uId ?? 0;
                    }
                }

                int zfmId = request.ZFM_ID;
                if (zfmId <= 0 && !string.IsNullOrWhiteSpace(request.ZFM))
                {
                    if (int.TryParse(request.ZFM, out int parsedId)) zfmId = parsedId;
                    else
                    {
                        var uId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 User_ID FROM dbo.User_Registration WHERE User_Name = @Name", new { Name = request.ZFM.Trim() });
                        zfmId = uId ?? 0;
                    }
                }

                int lpId = request.LP_ID;
                if (lpId <= 0 && !string.IsNullOrWhiteSpace(request.LP))
                {
                    if (int.TryParse(request.LP, out int parsedId)) lpId = parsedId;
                    else
                    {
                        var uId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT TOP 1 User_ID FROM dbo.User_Registration WHERE User_Name = @Name", new { Name = request.LP.Trim() });
                        lpId = uId ?? 0;
                    }
                }

                // 3. Bind all parameters supported by the new DB's SP_Master (no unknown @State or @City)
                var parameters = new DynamicParameters();
                parameters.Add("@Status", status);
                parameters.Add("@Store_Code", request.Store_Code?.Trim() ?? string.Empty);
                parameters.Add("@Store_Name", request.Store_Name?.Trim() ?? string.Empty);
                parameters.Add("@Entry_By", request.Entry_By);
                parameters.Add("@Is_Status", request.Is_Status);
                parameters.Add("@Reader_Id", request.Reader_Id);
                parameters.Add("@Reader_Name", request.Reader_Name?.Trim() ?? string.Empty);
                parameters.Add("@Reader_MAC", request.Reader_MAC?.Trim() ?? string.Empty);
                parameters.Add("@Antena", request.Antena);
                parameters.Add("@User_Name", request.User_Name?.Trim() ?? string.Empty);
                parameters.Add("@Password", request.Password?.Trim() ?? string.Empty);
                parameters.Add("@User_Type", request.User_Type?.Trim() ?? string.Empty);
                parameters.Add("@Reader_Config_ID", request.Reader_Config_ID);
                parameters.Add("@Modify_By", request.Modify_By);
                parameters.Add("@Store_ID", request.Store_ID);
                parameters.Add("@Store_Floor_ID", request.Store_Floor_ID);
                parameters.Add("@Store_Floor", request.Store_Floor?.Trim() ?? string.Empty);

                // For user directory, ensure an authorized Super Admin ID is used if 0
                int effectiveUserId = request.User_ID;
                if (effectiveUserId <= 0 && status.Equals("SP_Bind_User_Master", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveUserId = 26; // Default active Super Admin handle on new DB
                }
                parameters.Add("@User_ID", effectiveUserId);

                parameters.Add("@Device_ESN", request.Device_ESN?.Trim() ?? string.Empty);
                parameters.Add("@Encode_DateTime", request.Encode_DateTime?.Trim() ?? string.Empty);
                parameters.Add("@WH_ID", request.WH_ID);
                parameters.Add("@Wh_Code", request.Wh_Code?.Trim() ?? string.Empty);
                parameters.Add("@Wh_Name", request.Wh_Name?.Trim() ?? string.Empty);
                parameters.Add("@Wh_Address", request.Wh_Address?.Trim() ?? string.Empty);

                // New DB specific mapped parameters
                string emailVal = !string.IsNullOrWhiteSpace(request.MAIL_ID) ? request.MAIL_ID.Trim() : (request.Email_ID?.Trim() ?? string.Empty);
                int emailFlag = request.Email_Required_Flag != 0 ? request.Email_Required_Flag : (request.Is_Email_Required ? 1 : 0);

                parameters.Add("@MAIL_ID", emailVal);
                parameters.Add("@State_ID", stateId);
                parameters.Add("@City_ID", cityId);
                parameters.Add("@SM_ID", smId);
                parameters.Add("@AM_ID", amId);
                parameters.Add("@ZFM_ID", zfmId);
                parameters.Add("@LP_ID", lpId);
                parameters.Add("@RoleName", request.RoleName?.Trim() ?? string.Empty);
                parameters.Add("@State_Name", !string.IsNullOrWhiteSpace(request.State_Name) ? request.State_Name.Trim() : (request.State?.Trim() ?? string.Empty));
                parameters.Add("@Emp_Code", request.Emp_Code?.Trim() ?? string.Empty);
                parameters.Add("@Emp_Name", request.Emp_Name?.Trim() ?? string.Empty);
                parameters.Add("@Role_ID", request.Role_ID);
                parameters.Add("@Emp_ID", request.Emp_ID);
                parameters.Add("@UserType", request.User_Type?.Trim() ?? string.Empty);
                parameters.Add("@CounterRoleid", !string.IsNullOrWhiteSpace(request.CounterRoleid) ? request.CounterRoleid.Trim() : "4");
                parameters.Add("@CounterEmpID", request.CounterEmpID);
                parameters.Add("@CounterStoreID", request.CounterStoreID);
                parameters.Add("@Email_Required_Flag", emailFlag);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                var rawRows = (await connection.QueryAsync<dynamic>(
                    "SP_Master", 
                    parameters, 
                    commandType: CommandType.StoredProcedure, 
                    commandTimeout: 120
                )).ToList();

                // 4. Dictionary Translation: Map DB columns back to frontend expected properties
                var mappedRows = new List<IDictionary<string, object?>>();
                foreach (var item in rawRows)
                {
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    if (item is IDictionary<string, object> itemDict)
                    {
                        foreach (var kvp in itemDict)
                        {
                            dict[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        var props = ((object)item).GetType().GetProperties();
                        foreach (var p in props)
                        {
                            dict[p.Name] = p.GetValue(item);
                        }
                    }

                    // Store Directory mappings
                    if (dict.TryGetValue("State_Name", out var stName) && !dict.ContainsKey("State"))
                        dict["State"] = stName;
                    if (dict.TryGetValue("City_Name", out var ctName) && !dict.ContainsKey("City"))
                        dict["City"] = ctName;
                    if (dict.TryGetValue("SM_NAME", out var sName))
                    {
                        dict["Store_Manager"] = sName;
                        dict["StoreManager"] = sName;
                    }
                    if (dict.TryGetValue("AM_NAME", out var aName))
                    {
                        dict["Area_Manager"] = aName;
                        dict["AreaManager"] = aName;
                    }
                    if (dict.TryGetValue("ZFM_NAME", out var zName))
                        dict["ZFM"] = zName;
                    if (dict.TryGetValue("LP_NAME", out var lName))
                        dict["LP"] = lName;

                    // User Directory mappings
                    if (dict.TryGetValue("User_Email_ID", out var uEmail))
                    {
                        dict["Email_ID"] = uEmail;
                        dict["Email"] = uEmail;
                    }
                    if (dict.TryGetValue("Email_Required_Flag", out var eFlag))
                    {
                        dict["Is_Email_Required"] = eFlag;
                        dict["IsEmailRequired"] = eFlag;
                    }

                    // Floor Directory mappings
                    if (dict.TryGetValue("STORE_NAME", out var strName) && !dict.ContainsKey("Store_Name"))
                        dict["Store_Name"] = strName;

                    mappedRows.Add(dict);
                }

                string message = parameters.Get<string>("@Message")?.Trim() ?? string.Empty;

                bool isSuccess = true;
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("INVALID USER", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("INCORRECT PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("USER IS INACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        isSuccess = false;
                    }
                }

                if (string.IsNullOrEmpty(message))
                {
                    message = mappedRows.Count > 0 ? $"Retrieved {mappedRows.Count} record(s) successfully." : "Operation executed.";
                }

                // If store status was updated/deleted/inserted, immediately evict Tier 2 caches and trigger background refresh
                if (isSuccess && !string.IsNullOrEmpty(request.Status) &&
                    (request.Status.Equals("Delete_tbl_Store_Master", StringComparison.OrdinalIgnoreCase) ||
                     request.Status.Equals("Update_tbl_Store_Master", StringComparison.OrdinalIgnoreCase) ||
                     request.Status.Equals("Insert_tbl_Store_Master", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("MasterService: Store master modified with status {Status}. Evicting Tier 2 dashboard caches and triggering warmup.", request.Status);

                    BaseDashboardService.InvalidateKeysByPrefix(_cache, 
                        "SaleDashboard_Master_", 
                        "VoidDashboard_Master_", 
                        "ReturnDashboard_Master_", 
                        "TagCycleCount_Master_");

                    CacheWarmerService.TriggerTier2WarmupWithReset();
                }

                return new MasterResponse
                {
                    Success = isSuccess,
                    Message = message,
                    Data = mappedRows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SP_Master with Status: {Status}", request.Status);
                throw;
            }
        }

        public async Task<StoreDropdownOptionsDto> GetStoreDropdownOptionsAsync()
        {
            var result = new StoreDropdownOptionsDto();
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // 1. States & Cities (Check tbl_State_Mst / tbl_City_Mst first, fallback to tbl_Store_Master)
                IEnumerable<(string? State, string? City)> stateCityRows;
                try
                {
                    stateCityRows = await connection.QueryAsync<(string? State, string? City)>(
                        @"SELECT DISTINCT STM.State_Name AS State, CM.City_Name AS City 
                          FROM dbo.tbl_City_Mst CM WITH (NOLOCK)
                          JOIN dbo.tbl_State_Mst STM WITH (NOLOCK) ON CM.State_ID = STM.State_ID
                          WHERE STM.State_Name IS NOT NULL AND STM.State_Name <> ''");
                }
                catch
                {
                    stateCityRows = await connection.QueryAsync<(string? State, string? City)>(
                        @"SELECT DISTINCT ISNULL(State, '') AS State, ISNULL(City, '') AS City 
                          FROM dbo.tbl_Store_Master WITH (NOLOCK)
                          WHERE State IS NOT NULL AND State <> ''");
                }

                var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in stateCityRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.State))
                    {
                        states.Add(row.State.Trim());
                        if (!string.IsNullOrWhiteSpace(row.City))
                        {
                            result.Cities.Add(new StateCityDto
                            {
                                State = row.State.Trim(),
                                City = row.City.Trim()
                            });
                        }
                    }
                }
                result.States = states.OrderBy(s => s).ToList();

                // 2. Area Managers
                try
                {
                    var amRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT User_Name FROM dbo.User_Registration WITH (NOLOCK)
                          WHERE (Role_ID = 1 OR User_Type LIKE '%Area%') AND Is_Status = 1");
                    result.AreaManagers = amRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }
                catch
                {
                    var amRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT Area_Manager FROM dbo.tbl_Store_Master WITH (NOLOCK)
                          WHERE Area_Manager IS NOT NULL AND Area_Manager <> ''");
                    result.AreaManagers = amRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }

                // 3. ZFMs
                try
                {
                    var zfmRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT User_Name FROM dbo.User_Registration WITH (NOLOCK)
                          WHERE (Role_ID = 2 OR User_Type LIKE '%ZFM%' OR User_Type LIKE '%Zonal%') AND Is_Status = 1");
                    result.ZFMs = zfmRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }
                catch
                {
                    var zfmRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT ZFM FROM dbo.tbl_Store_Master WITH (NOLOCK)
                          WHERE ZFM IS NOT NULL AND ZFM <> ''");
                    result.ZFMs = zfmRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }

                // 4. LPs
                try
                {
                    var lpRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT User_Name FROM dbo.User_Registration WITH (NOLOCK)
                          WHERE (Role_ID = 3 OR User_Type LIKE '%LP%' OR User_Type LIKE '%Loss%') AND Is_Status = 1");
                    result.LPs = lpRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }
                catch
                {
                    var lpRows = await connection.QueryAsync<string>(
                        @"SELECT DISTINCT LP FROM dbo.tbl_Store_Master WITH (NOLOCK)
                          WHERE LP IS NOT NULL AND LP <> ''");
                    result.LPs = lpRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();
                }

                // 5. Store Managers (Store Admin usernames excluding pure numeric employee IDs)
                var smRows = await connection.QueryAsync<string>(
                    @"SELECT DISTINCT User_Name 
                      FROM dbo.User_Registration WITH (NOLOCK)
                      WHERE (User_Type = 'Store Admin' OR Role_ID = 5) 
                        AND Is_Status = 1 
                        AND User_Name LIKE '%[A-Za-z]%' 
                      ORDER BY User_Name");
                result.StoreManagers = smRows.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching store dropdown options from database.");
                return result;
            }
        }
    }
}

