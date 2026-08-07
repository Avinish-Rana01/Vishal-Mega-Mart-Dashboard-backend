using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.SqlClient;
using System.Data;
using VS_Mart_Backend.Models;

namespace VS_Mart_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("POS") ?? throw new Exception("Database connection string missing.");
        }


        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            try
            {
                DataSet ds1 = new DataSet();
                string dbMessage = string.Empty;

                using (SqlConnection sqlcon = new SqlConnection(_configuration.GetConnectionString("POS")))
                {
                    sqlcon.Open();


                    using (SqlCommand cmd = new SqlCommand("SP_Master", sqlcon))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@User_Name", request.UserName.Trim());
                        cmd.Parameters.AddWithValue("@Password", request.Password.Trim());
                        cmd.Parameters.AddWithValue("@Status", "SP_Login");

                        SqlParameter msgParam = new SqlParameter("@Message", SqlDbType.VarChar, 200);
                        msgParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(msgParam);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds1);

                        dbMessage = msgParam.Value?.ToString();
                    }
                }

                // No Record Found
                if (ds1 == null || ds1.Tables.Count == 0 || ds1.Tables[0].Rows.Count == 0)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                DataRow row = ds1.Tables[0].Rows[0];

                string userType = row["User_Type"].ToString().Trim();

                // Invalid User Type
                if (userType == "Store" || userType == "Warehouse")
                {
                    return Forbid();
                }

                string redirectPage = "Dashboard";

                if (userType == "Dispatch Admin")
                    redirectPage = "Dispatch_Tracking";

                else if (userType == "Tag Admin")
                    redirectPage = "Tag_Cycle_Count";

                LoginResponse response = new LoginResponse
                {
                    Success = true,
                    Message = "Login Successful",

                    UserName = row["User_Name"].ToString(),
                    UserID = row["User_ID"].ToString(),
                    UserType = userType,
                    StoreName = row["STORE_NAME"].ToString(),
                    WarehouseName = row["WH_NAME"].ToString(),
                    StoreCode = row["Store_Code"].ToString(),
                    WarehouseCode = row["WH_Code"].ToString(),

                    RedirectPage = redirectPage
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores.");
                return StatusCode(500, "An error occurred while loading stores.");
            }
           
            
        }

    }
}
