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

    public class StoreDropdownItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ArticleSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? StoreCode { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }

    public class ArticleItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class LiveStockReportRequest
    {
        public string? SearchTerm { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? StoreName { get; set; }
        public string? StockDate { get; set; }
        public string? ArticleNo { get; set; }
        public string? SortColumn { get; set; } = "STOCK_DATE";
        public string? SortDirection { get; set; } = "asc";
    }

    public class LiveStockReportResponse
    {
        public ReportSummary Summary { get; set; } = new();
        public List<Dictionary<string, object?>> Data { get; set; } = new();
    }

    public class ReportSummary
    {
        public int PageIndex { get; set; }
        public int TotalRecords { get; set; }
        public int SapStockCount { get; set; }
        public int RfidStockCount { get; set; }
        public int DifferenceCount { get; set; }
    }

    #endregion

    [ApiController]
    [Route("api/report")] // Base route for all 3 APIs (e.g., api/report/stores)
    public class ModernReportController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ModernReportController> _logger;

        public ModernReportController(IConfiguration configuration, ILogger<ModernReportController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("POS") ?? throw new Exception("Database connection string missing.");
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetStores([FromQuery] string? userId, [FromQuery] string? fromDate, [FromQuery] string? toDate)
        {
            try
            {
                var list = new List<StoreDropdownItem>();

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@status", "BIND_STORE_FOR_ALL");
                    cmd.Parameters.AddWithValue("@USER_ID", (object?)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                    await con.OpenAsync();
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var storeCode = reader["STORE_CODE"]?.ToString() ?? "";
                            list.Add(new StoreDropdownItem { Text = storeCode, Value = storeCode });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores.");
                return StatusCode(500, "An error occurred while loading stores."); 
            }
        }

        [HttpGet("articles/search")]
        public async Task<IActionResult> SearchArticles([FromQuery] ArticleSearchRequest request)
        {
            try
            {
                var list = new List<ArticleItem>();

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    string status = string.IsNullOrEmpty(request.SearchTerm) 
                        ? "BIND_MATERIAL_FOR_LIVE_STOCK" 
                        : "SEARCH_BIND_MATERIAL_FOR_LIVE_STOCK";

                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object?)request.SearchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Store_Code", (object?)request.StoreCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@fromdate", (object?)request.FromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@todate", (object?)request.ToDate ?? DBNull.Value);

                    await con.OpenAsync();
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var material = reader["MATERIAL"]?.ToString() ?? "";
                            list.Add(new ArticleItem { Id = material, Text = material });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching articles.");
                return StatusCode(500, "An error occurred while searching for articles.");
            }
        }

        [HttpGet("live-stock")]
        public async Task<IActionResult> GetLiveStockDetails([FromQuery] LiveStockReportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StoreName) || 
                string.IsNullOrWhiteSpace(request.StockDate))
            {
                return BadRequest("Store Name and Stock Date are required."); 
            }

            try
            {
                var response = new LiveStockReportResponse();

                using (var con = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand("SP_NEW_REPORT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@status", "LIVE_STOCK_REPORT");
                    cmd.Parameters.AddWithValue("@SearchTerm", request.SearchTerm ?? "");
                    cmd.Parameters.AddWithValue("@PageIndex", request.PageIndex);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@Store_Code", request.StoreName ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", request.StockDate ?? "");
                    cmd.Parameters.AddWithValue("@todate", request.StockDate ?? "");
                    cmd.Parameters.AddWithValue("@Material", request.ArticleNo ?? "");
                    cmd.Parameters.AddWithValue("@SortColumn", request.SortColumn ?? "STOCK_DATE");
                    cmd.Parameters.AddWithValue("@SortDirection", request.SortDirection ?? "asc");

                    var pRecordCount = new SqlParameter("@RecordCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pQty = new SqlParameter("@QTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pEncQty = new SqlParameter("@ENCQTY", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pDiffQty = new SqlParameter("@DIFFQTY", SqlDbType.Int) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(pRecordCount);
                    cmd.Parameters.Add(pQty);
                    cmd.Parameters.Add(pEncQty);
                    cmd.Parameters.Add(pDiffQty);

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

                    response.Summary.PageIndex = request.PageIndex;
                    response.Summary.TotalRecords = pRecordCount.Value != DBNull.Value ? Convert.ToInt32(pRecordCount.Value) : 0;
                    response.Summary.SapStockCount = pQty.Value != DBNull.Value ? Convert.ToInt32(pQty.Value) : 0;
                    response.Summary.RfidStockCount = pEncQty.Value != DBNull.Value ? Convert.ToInt32(pEncQty.Value) : 0;
                    response.Summary.DifferenceCount = pDiffQty.Value != DBNull.Value ? Convert.ToInt32(pDiffQty.Value) : 0;
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching live stock details.");
                return StatusCode(500, "An error occurred while loading the report data.");
            }
        }
    }
}
