using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [RoutePrefix("api/reports")]
    public class ReportsController : ApiController
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
                    revenue = ScalarInt(conn, "SELECT ISNULL(SUM(Total),0) FROM Orders WHERE Status=N'Hoàn thành'"),
                    profit = ScalarInt(conn, @"SELECT ISNULL(SUM((p.Price - p.Cost) * o.Qty),0)
                    FROM Orders o
                    JOIN Products p ON CHARINDEX(p.Name, o.Product) > 0
                    WHERE o.Status=N'Hoàn thành'"),
                    ordersByStatus = Query(conn, "SELECT Status, COUNT(*) AS Count FROM Orders GROUP BY Status ORDER BY Status"),
                    revenueByDay = Query(conn, "SELECT CONVERT(varchar(10), OrderDate, 23) AS [Date], SUM(Total) AS Revenue FROM Orders GROUP BY OrderDate ORDER BY OrderDate"),
                    topProducts = Query(conn, "SELECT TOP 5 Product, SUM(Qty) AS Qty, SUM(Total) AS Revenue FROM Orders GROUP BY Product ORDER BY SUM(Total) DESC"),
                    inventoryValue = ScalarInt(conn, "SELECT ISNULL(SUM(Cost * Stock),0) FROM Products")
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
