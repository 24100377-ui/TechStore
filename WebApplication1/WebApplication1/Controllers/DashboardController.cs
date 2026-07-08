using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    [Authorize]
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return Ok(new
                {
                    totalProducts = ScalarInt(conn, "SELECT COUNT(*) FROM Products"),
                    totalCustomers = ScalarInt(conn, "SELECT COUNT(*) FROM Customers"),
                    totalOrders = ScalarInt(conn, "SELECT COUNT(*) FROM Orders"),
                    revenue = ScalarInt(conn, "SELECT ISNULL(SUM(Total),0) FROM Orders WHERE Status=N'Hoàn thành'"),
                    pendingOrders = ScalarInt(conn, "SELECT COUNT(*) FROM Orders WHERE Status=N'Chờ xử lý'"),
                    lowStock = Query(conn, "SELECT TOP 10 Id, Name, Stock FROM Products WHERE Stock <= 5 ORDER BY Stock ASC"),
                    recentOrders = Query(conn, "SELECT TOP 5 Id, Customer, Product, Total, Status, CONVERT(varchar(10), OrderDate, 23) AS [Date] FROM Orders ORDER BY OrderDate DESC, Id DESC")
                });
            }
        }

        private static int ScalarInt(SqlConnection conn, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn)) return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static List<Dictionary<string, object>> Query(SqlConnection conn, string sql)
        {
            var rows = new List<Dictionary<string, object>>();
            using (var cmd = new SqlCommand(sql, conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (var i = 0; i < rd.FieldCount; i++) row[rd.GetName(i)] = rd.GetValue(i);
                    rows.Add(row);
                }
            }
            return rows;
        }
    }
}
