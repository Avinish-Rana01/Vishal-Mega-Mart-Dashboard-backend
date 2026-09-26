using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace VS_Mart_Backend.Features.Dashboard.Export
{
    public class ReportConfig
    {
        public string ReportName { get; set; } = string.Empty;
        public string StoredProcedure { get; set; } = "SP_NEW_REPORT";
        public string Status { get; set; } = string.Empty;
        public string DefaultSortColumn { get; set; } = "DATE";
        public string DefaultSortDirection { get; set; } = "DESC";
        public Action<SqlCommand, UniversalExportRequest> ParameterBinder { get; set; } = (_, _) => { };
    }

    public static class ReportRegistry
    {
        private static readonly Dictionary<string, ReportConfig> _registry = new(StringComparer.OrdinalIgnoreCase);

        static ReportRegistry()
        {
            RegisterReports();
        }

        public static ReportConfig? GetConfig(string reportName)
        {
            if (string.IsNullOrWhiteSpace(reportName)) return null;
            _registry.TryGetValue(reportName.Trim(), out var config);
            return config;
        }

        public static bool Contains(string reportName) =>
            !string.IsNullOrWhiteSpace(reportName) && _registry.ContainsKey(reportName.Trim());

        private static void RegisterReports()
        {
            // Helper for clean dates
            static string CleanDate(string? d, string fallback) =>
                string.IsNullOrWhiteSpace(d) ? fallback : d.Trim().Trim('"');

            static string EffectiveStore(UniversalExportRequest r) =>
                !string.IsNullOrWhiteSpace(r.StoreCode) ? r.StoreCode.Trim() : (r.StoreName ?? "").Trim();

            // ==============================================================
            // 1. VENDOR HU DISCREPANCY SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "VENDOR_HU_DISCREPANCY_SUMMARY",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "VIEW_PARK_HU_VENDOR_REPORT",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Vendor_Code", string.IsNullOrWhiteSpace(r.VendorCode) ? "0" : r.VendorCode.Trim());
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 2. HU SUMMARY VALIDATION (DC Validation)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "HU_SUMMARY_VALIDATION",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "HU_VALIDATION_REPORT",
                DefaultSortColumn = "ENCODE_DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    string hu = (r.HuNo == "ALL HU" || string.IsNullOrWhiteSpace(r.HuNo)) ? "" : r.HuNo.Trim();
                    cmd.Parameters.AddWithValue("@HU", hu);
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 3. HU DETAILS (DC Inward)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "HU_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "HU_DETAILS",
                DefaultSortColumn = "HU_Number",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Reciving_Plant", r.ReceivingPlant ?? "");
                    cmd.Parameters.AddWithValue("@CI_STATUS", r.HuStatus ?? "");
                    cmd.Parameters.AddWithValue("@HU_NO", r.HuNo ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 4. HU REPORT VIEW DETAILS (Drilldown)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "HU_REPORT_VIEW_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "HU_REPORT_DETAILS",
                DefaultSortColumn = "HU_Number",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@ref_No", r.RefNo ?? "");
                    cmd.Parameters.AddWithValue("@CI_STATUS", r.HuStatus ?? "1");
                    cmd.Parameters.AddWithValue("@HU_NO", r.HuNo ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 5. DC VALIDATION DETAILS
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "DC_VALIDATION_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "DC_VALIDATION_DETAILS",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 6. TOTAL DPOS SALE SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "TOTAL_DPOS_SALE_SUMMARY",
                StoredProcedure = "SP_NEW_DASHBOARD",
                Status = "LAST7DAY_SALE_DASHBOARD",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 7. TOTAL DPOS SALE DATA (Grid Breakdown)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "TOTAL_DPOS_SALE_DATA",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_POS_SALE_DATA",
                DefaultSortColumn = "ITEM_CD",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.Material ?? r.ArticleNo ?? "");
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 8. RFID CHECKOUT SALE DATA
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "RFID_CHECKOUT_SALE_DATA",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_RFID_CHECKOUT_DATA",
                DefaultSortColumn = "ITEM_CD",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.Material ?? r.ArticleNo ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 9. MANUAL SALE DATA
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "MANUAL_SALE_DATA",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_MANUAL_SALE_DATA",
                DefaultSortColumn = "ITEM_CD",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.Material ?? r.ArticleNo ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 10. STORE SALE REPORT
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "STORE_SALE_REPORT",
                StoredProcedure = "SP_NEW_DASHBOARD",
                Status = "LAST7DAY_SALE_DASHBOARD",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 11. VOID DETAILS
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "VOID_DETAILS",
                StoredProcedure = "SP_NEW_DASHBOARD",
                Status = "LAST7DAY_VOID_DASHBOARD",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 12. VOID RECONCILIATION SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "VOID_RECONCILIATION_SUMMARY",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_SUMMARY_FOR_VOID",
                DefaultSortColumn = "VOID_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@STORE_CODE", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 13. VOID RECONCILIATION ITEM DETAILS (Modal)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "VOID_RECONCILIATION_ITEM_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_SUMMARY_DATA_FOR_VOID",
                DefaultSortColumn = "VOID_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@STORE_CODE", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@BILL_DATE", r.BillDate ?? "");
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 14. RETURN DETAILS
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "RETURN_DETAILS",
                StoredProcedure = "SP_NEW_DASHBOARD",
                Status = "LAST7DAY_RETURN_DASHBOARD",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 15. RETURN RECONCILIATION SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "RETURN_RECONCILIATION_SUMMARY",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_SUMMARY_FOR_RETURN",
                DefaultSortColumn = "BILL_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@STORE_CODE", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 16. RETURN RECONCILIATION ITEM DETAILS (Modal)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "RETURN_RECONCILIATION_ITEM_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_SUMMARY_DATA_FOR_RETURN",
                DefaultSortColumn = "BILL_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@STORE_CODE", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@BILL_DATE", r.BillDate ?? "");
                    cmd.Parameters.AddWithValue("@COUNTER_NO", r.Pos ?? "");
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 17. STORE GRC SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "STORE_GRC_SUMMARY",
                StoredProcedure = "SP_NEW_DASHBOARD",
                Status = "LAST7DAY_STORE_DASHBOARD",
                DefaultSortColumn = "GRC_DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 18. GRC DETAILS
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "GRC_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_GRC_DATA",
                DefaultSortColumn = "GRC_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    string grcStatus = r.GrcStatus ?? "1";
                    if (grcStatus == "0" || grcStatus == "2")
                    {
                        cmd.Parameters["@status"].Value = "SHOW_HHTGRC_DATA";
                        cmd.Parameters.AddWithValue("@GRC_STATUS", grcStatus == "0" ? "2" : "1");
                    }
                    else if (grcStatus == "3")
                    {
                        cmd.Parameters["@status"].Value = "SHOW_STORE_PENDING_GRC_DATA";
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@GRC_STATUS", grcStatus);
                    }

                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@HU_NO", r.HuNo ?? "");
                    cmd.Parameters.AddWithValue("@FromDate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@ToDate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 19. GRC ARTICLE ITEM DETAILS (Modal)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "GRC_ARTICLE_ITEM_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_GRC_MODAL_DATA",
                DefaultSortColumn = "Scan_Date",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_Code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@HU_NO", r.HuNo ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.Article ?? r.ArticleNo ?? "");
                    cmd.Parameters.AddWithValue("@ScanTime", r.ScanTime ?? "");
                    cmd.Parameters.AddWithValue("@GRC_STATUS", r.GrcStatus ?? "1");
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 20. CYCLE COUNT SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "CYCLE_COUNT_SUMMARY",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "CYCLE_COUNT_REPORT_VIEW",
                DefaultSortColumn = "DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@FromDate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@ToDate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 21. CYCLE COUNT AUDIT DETAILS (Ref Drilldown)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "CYCLE_COUNT_AUDIT_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "CYCLE_COUNT_REPORT",
                DefaultSortColumn = "STORE_CODE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@ref_No", r.RefNo ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 22. WAREHOUSE ENCODING SUMMARY
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "WAREHOUSE_ENCODING_SUMMARY",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "SHOW_WAREHOUSE_ENCODE_DATA",
                DefaultSortColumn = "ENCODE_DATE",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    int uid = 0;
                    if (!string.IsNullOrWhiteSpace(r.User) && int.TryParse(r.User, out var parsed)) uid = parsed;
                    else if (r.UserId.HasValue) uid = r.UserId.Value;

                    cmd.Parameters.AddWithValue("@User_ID", uid);
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 23. ALLOCATED STORE ENCODING
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "ALLOCATED_STORE_ENCODING",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "ENCODING_STORE_DATA",
                DefaultSortColumn = "ARTICLE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.ArticleNo ?? r.Article ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 24. ALLOCATED STORE ITEM DETAILS (Modal)
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "ALLOCATED_STORE_ITEM_DETAILS",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "VIEW_ENCODING_SHOW_DATA_FOR_STORE",
                DefaultSortColumn = "Encode_Date",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@EAN", r.Ean ?? "");
                    cmd.Parameters.AddWithValue("@Material", r.ArticleNo ?? r.Article ?? "");
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, "2000-01-01"));
                    cmd.Parameters.AddWithValue("@todate", CleanDate(r.ToDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 25. TAG INVENTORY DISTRIBUTION
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "TAG_INVENTORY_DISTRIBUTION",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "TAG_MANAGEMENT_LOCATION",
                DefaultSortColumn = "CYCLE_COUNT",
                DefaultSortDirection = "DESC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });

            // ==============================================================
            // 26. LIVE STOCK REPORT
            // ==============================================================
            Register(new ReportConfig
            {
                ReportName = "LIVE_STOCK_REPORT",
                StoredProcedure = "SP_NEW_REPORT",
                Status = "LIVE_STOCK_REPORT",
                DefaultSortColumn = "STOCK_DATE",
                DefaultSortDirection = "ASC",
                ParameterBinder = (cmd, r) =>
                {
                    cmd.Parameters.AddWithValue("@Store_code", EffectiveStore(r));
                    cmd.Parameters.AddWithValue("@fromdate", CleanDate(r.FromDate, DateTime.Today.ToString("yyyy-MM-dd")));
                    cmd.Parameters.AddWithValue("@Material", r.ArticleNo ?? r.Article ?? "");
                    cmd.Parameters.AddWithValue("@SearchTerm", r.SearchTerm ?? "");
                }
            });
        }

        private static void Register(ReportConfig config)
        {
            _registry[config.ReportName] = config;
        }
    }
}
