using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [RoutePrefix("api/stock")]
    public class StockController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpPost, Route("")]
        public IHttpActionResult UpdateStock(StockRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductId) || request.Qty <= 0) return BadRequest("Du lieu ton kho khong hop le.");
            var delta = request.Action == "out" ? -request.Qty : request.Qty;

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
            DECLARE @Rows INT;
            UPDATE Products SET Stock = Stock + @Delta WHERE Id = @Id AND Stock + @Delta >= 0;
            SET @Rows = @@ROWCOUNT;
            IF @Rows = 0
                SELECT -1;
            ELSE
                SELECT Stock FROM Products WHERE Id = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Delta", delta);
                cmd.Parameters.AddWithValue("@Id", request.ProductId);
                conn.Open();
                var stock = Convert.ToInt32(cmd.ExecuteScalar());
                if (stock < 0) return BadRequest("Khong du hang hoac san pham khong ton tai.");
                return Ok(new { productId = request.ProductId, stock });
            }
        }
    }
}
