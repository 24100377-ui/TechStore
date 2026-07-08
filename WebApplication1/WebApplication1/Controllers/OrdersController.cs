using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var items = new List<OrderDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT o.Id, o.CustomerId, o.ProductId,
                       c.Name AS CustomerName, p.Name AS ProductName,
                       o.Qty, o.Total, o.Status, o.OrderDate
                FROM Orders o
                LEFT JOIN Customers c ON c.Id = o.CustomerId
                LEFT JOIN Products  p ON p.Id = o.ProductId
                ORDER BY o.Id", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read()) items.Add(ReadOrder(rd));
                }
            }
            return Ok(items);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(OrderDto order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerId))
                return BadRequest("CustomerId la bat buoc.");
            if (string.IsNullOrWhiteSpace(order.ProductId))
                return BadRequest("ProductId la bat buoc.");
            if (string.IsNullOrWhiteSpace(order.Id)) order.Id = NextId();
            if (string.IsNullOrWhiteSpace(order.Status)) order.Status = "Chờ xử lý";
            if (string.IsNullOrWhiteSpace(order.Date)) order.Date = DateTime.Today.ToString("yyyy-MM-dd");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Lấy giá sản phẩm nếu Total chưa được tính
                        if (order.Total <= 0)
                        {
                            using (var cmd = new SqlCommand(
                                "SELECT Price FROM Products WHERE Id=@Id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Id", order.ProductId);
                                var price = cmd.ExecuteScalar();
                                if (price == null) return BadRequest("San pham khong ton tai.");
                                order.Total = Convert.ToInt32(price) * order.Qty;
                            }
                        }

                        using (var cmd = new SqlCommand(@"
                            INSERT INTO Orders(Id, CustomerId, ProductId, Customer, Product, Qty, Total, Status, OrderDate)
                            SELECT @Id, @CustomerId, @ProductId,
                                   c.Name, p.Name,
                                   @Qty, @Total, @Status, @OrderDate
                            FROM Customers c, Products p
                            WHERE c.Id=@CustomerId AND p.Id=@ProductId", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", order.Id);
                            cmd.Parameters.AddWithValue("@CustomerId", order.CustomerId);
                            cmd.Parameters.AddWithValue("@ProductId", order.ProductId);
                            cmd.Parameters.AddWithValue("@Qty", order.Qty);
                            cmd.Parameters.AddWithValue("@Total", order.Total);
                            cmd.Parameters.AddWithValue("@Status", order.Status);
                            cmd.Parameters.AddWithValue("@OrderDate", DateTime.Parse(order.Date));
                            if (cmd.ExecuteNonQuery() == 0)
                                return BadRequest("Khach hang hoac san pham khong ton tai.");
                        }

                        // Cập nhật thống kê customer theo FK (không theo tên)
                        using (var cmd = new SqlCommand(@"
                            UPDATE Customers
                            SET Orders = Orders + 1,
                                Spent  = Spent  + @Spent
                            WHERE Id = @CustomerId", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Spent", order.Total);
                            cmd.Parameters.AddWithValue("@CustomerId", order.CustomerId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
            return Ok(order);
        }

        [HttpPut, Route("{id}/status")]
        public IHttpActionResult UpdateStatus(string id, UpdateOrderStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status)) return BadRequest("Trang thai la bat buoc.");
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("UPDATE Orders SET Status=@Status WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Status", request.Status);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok(new { id, status = request.Status });
        }

        private static string NextId()
        {
            return "ORD" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        private static OrderDto ReadOrder(SqlDataReader rd)
        {
            return new OrderDto
            {
                Id = rd["Id"].ToString(),
                CustomerId = rd["CustomerId"]?.ToString(),
                ProductId = rd["ProductId"]?.ToString(),
                Customer = rd["CustomerName"]?.ToString(),
                Product = rd["ProductName"]?.ToString(),
                Qty = Convert.ToInt32(rd["Qty"]),
                Total = Convert.ToInt32(rd["Total"]),
                Status = rd["Status"].ToString(),
                Date = Convert.ToDateTime(rd["OrderDate"]).ToString("yyyy-MM-dd")
            };
        }
    }
}