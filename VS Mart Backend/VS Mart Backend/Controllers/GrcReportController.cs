using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Controllers
{
    // ==============================================================================
    // THE MENU (DTOs - Data Transfer Objects)
    // ==============================================================================
    #region DTOs

    public class HuNumberItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class GrcHuSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? GrcStatus { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? StoreCode { get; set; }
    }

    public class GrcDetailsRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? GrcStatus { get; set; }
        public string? StoreName { get; set; }
        public string? HuNo { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? SortColumn { get; set; } = "GRC_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class GrcDetailsResponse
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class GrcModalDetailsRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "GRC_DATE";
        public string? SortDirection { get; set; } = "asc";
        public string? Date { get; set; }
        public string? StoreCode { get; set; }
        public string? HuNumber { get; set; }
        public string? GrcStatus { get; set; }
    }

    public class GrcModalDetailsResponse
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public int Qty { get; set; }
        public int MaterialCount { get; set; }
        public int ActualQty { get; set; }
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    #endregion

    [ApiController]
    [Route("api/grc-report")]
    public class GrcReportController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GrcReportController> _logger;

        public GrcReportController(IConfiguration configuration, ILogger<GrcReportController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("POS") ?? throw new Exception("Database connection string missing.");
        }

        // ==============================================================================
        // API 1: HU Number Autocomplete
        // ==============================================================================
        [HttpGet("hu-numbers/search")]
        public async Task<IActionResult> SearchHuNumbers([FromQuery] GrcHuSearchRequest request)
        {
            try
            {
                var list = new List<HuNumberItem>();

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    string status = "";
                    string grcStatus = request.GrcStatus ?? "";
                    bool isSearch = !string.IsNullOrEmpty(request.SearchTerm);

                    if (grcStatus == "" || grcStatus == "1")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_GRC" : "BIND_HU_FOR_GRC";
                        cmd.Parameters.AddWithValue("@GRC_STATUS", grcStatus);
                    }
                    else if (grcStatus == "0")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_HHTGRC" : "BIND_HU_FOR_HHTGRC";
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "2");
                    }
                    else if (grcStatus == "2")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_HHTGRC" : "BIND_HU_FOR_HHTGRC";
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "1");
                    }
                    else if (grcStatus == "3")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_STORE_PENDING_GRC" : "BIND_HU_FOR_STORE_PENDING_GRC";
                    }
                    else if (grcStatus == "4")
                    {
                        status = isSearch ? "SEARCH_BIND_HU_FOR_GRC" : "BIND_HU_FOR_GRC";
                    }

                    cmd.Parameters.AddWithValue("@status", status);
                    
                    // Critical Learning Rule: Always send "" instead of DBNull.Value for missing strings!
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@FromDate", request.FromDate ?? "");
                    cmd.Parameters.AddWithValue("@Todate", request.ToDate ?? "");
                    cmd.Parameters.AddWithValue("@Store_Code", request.StoreCode ?? "");

                    await con.OpenAsync();
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var hu = reader["HU"]?.ToString() ?? "";
                            list.Add(new HuNumberItem { Id = hu, Text = hu });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching HU numbers.");
                return StatusCode(500, "An error occurred while loading HU numbers.");
            }
        }

        // ==============================================================================
        // API 2: Main Grid Data
        // ==============================================================================
        [HttpGet("details")]
        public async Task<IActionResult> GetGrcDetails([FromQuery] GrcDetailsRequest request)
        {
            try
            {
                var response = new GrcDetailsResponse { PageIndex = request.PageIndex };

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    string grcStatus = request.GrcStatus ?? "";
                    
                    if (grcStatus == "" || grcStatus == "1")
                    {
                        cmd.Parameters.AddWithValue("@status", "SHOW_GRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", grcStatus);
                    }
                    else if (grcStatus == "0")
                    {
                        cmd.Parameters.AddWithValue("@status", "SHOW_HHTGRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "2");
                    }
                    else if (grcStatus == "2")
                    {
                        cmd.Parameters.AddWithValue("@status", "SHOW_HHTGRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "1");
                    }
                    else if (grcStatus == "3")
                    {
                        cmd.Parameters.AddWithValue("@status", "SHOW_STORE_PENDING_GRC_DATA");
                    }
                    else if (grcStatus == "4")
                    {
                        cmd.Parameters.AddWithValue("@status", "SHOW_GRC_DATA");
                    }

                    // Critical Learning Rule: Send "" for missing strings!
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@Store_Code", request.StoreName ?? "");
                    cmd.Parameters.AddWithValue("@FromDate", request.FromDate ?? "");
                    cmd.Parameters.AddWithValue("@ToDate", request.ToDate ?? "");
                    cmd.Parameters.AddWithValue("@HU_NO", request.HuNo ?? "");
                    cmd.Parameters.AddWithValue("@SortColumn", request.SortColumn ?? "GRC_DATE");
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                    var pRecordCount = new SqlParameter("@RecordCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pRecordCount);

                    await con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            response.Data.Add(row);
                        }
                    }

                    response.TotalRecords = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0;
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC details.");
                return StatusCode(500, "An error occurred while loading the report data.");
            }
        }

        // ==============================================================================
        // API 3: Modal Drill-Down Data
        // ==============================================================================
        [HttpGet("modal-details")]
        public async Task<IActionResult> GetGrcModalDetails([FromQuery] GrcModalDetailsRequest request)
        {
            try
            {
                var response = new GrcModalDetailsResponse { PageIndex = request.PageIndex };

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    string grcStatus = request.GrcStatus ?? "";
                    
                    if (grcStatus == "" || grcStatus == "1")
                    {
                        cmd.Parameters.AddWithValue("@status", "VIEW_SHOW_GRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", grcStatus);
                    }
                    else if (grcStatus == "0")
                    {
                        cmd.Parameters.AddWithValue("@status", "VIEW_SHOW_HHTGRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "2");
                    }
                    else if (grcStatus == "2")
                    {
                        cmd.Parameters.AddWithValue("@status", "VIEW_SHOW_HHTGRC_DATA");
                        cmd.Parameters.AddWithValue("@GRC_STATUS", "1");
                    }
                    else if (grcStatus == "3")
                    {
                        cmd.Parameters.AddWithValue("@status", "VIEW_SHOW_STORE_PENDING_GRC_DATA");
                    }
                    else if (grcStatus == "4")
                    {
                        cmd.Parameters.AddWithValue("@status", "VIEW_SHOW_GRC_DATA");
                    }

                    // Critical Learning Rule: Send "" for missing strings!
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@Store_Code", request.StoreCode ?? "");
                    cmd.Parameters.AddWithValue("@HU_NO", request.HuNumber ?? "");
                    cmd.Parameters.AddWithValue("@SortColumn", request.SortColumn ?? "GRC_DATE");
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                    // Map the single Date to both FromDate and ToDate just like the legacy WebForms code
                    if (DateTime.TryParse(request.Date, out DateTime parsedDate))
                    {
                        cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = parsedDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = parsedDate;
                    }
                    else
                    {
                        cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = DBNull.Value;
                        cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = DBNull.Value;
                    }

                    var pRecordCount = new SqlParameter("@RecordCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pQty = new SqlParameter("@QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pMaterialCount = new SqlParameter("@MATERIALCOUNT", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pActualQty = new SqlParameter("@ACTUALQTY", SqlDbType.Int) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(pRecordCount);
                    cmd.Parameters.Add(pQty);
                    cmd.Parameters.Add(pMaterialCount);
                    cmd.Parameters.Add(pActualQty);

                    await con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            response.Data.Add(row);
                        }
                    }

                    response.TotalRecords = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0;
                    response.Qty = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0;
                    response.MaterialCount = pMaterialCount.Value != DBNull.Value ? Convert.ToInt32(pMaterialCount.Value) : 0;
                    response.ActualQty = pActualQty.Value != DBNull.Value ? Convert.ToInt32(pActualQty.Value) : 0;
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GRC modal details.");
                return StatusCode(500, "An error occurred while loading the modal data.");
            }
        }
    }
}
