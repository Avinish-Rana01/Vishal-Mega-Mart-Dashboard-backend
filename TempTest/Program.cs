using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TempTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=172.20.204.184;Initial Catalog=VMM_RFID_RETAIL_SOLUTION;User ID=sa;Password=mil;Integrated Security=false;TrustServerCertificate=True;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                for (int userId = 0; userId <= 5; userId++)
                {
                    using (var cmd = new SqlCommand("[SP_New_Dashboard]", connection))
                    {
                        cmd.CommandText = "sp_helptext";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@objname", "SP_NEW_REPORT");
                        using (var reader = cmd.ExecuteReader())
                        {
                            using (var writer = new System.IO.StreamWriter("sp_report.txt"))
                            {
                                while (reader.Read())
                                {
                                    writer.Write(reader.GetString(0));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
