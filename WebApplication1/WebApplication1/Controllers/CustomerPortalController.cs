using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Các endpoint dành riêng cho khách hàng (user portal).
    /// Route prefix: /api/portal
    /// </summary>
    [RoutePrefix("api/portal")]
    public class CustomerPortalController : ApiController
    {
        private readonly string _connStr = ConfigurationManager
            .ConnectionStrings["DefaultConnection"].ConnectionString;

        // ─────────────────────────────────────────────────────────
        // AUTH (không cần token)
        // ─────────────────────────────────────────────────────────

        /// <summary>POST /api/portal/register</summary>
        [AllowAnonymous]
        [HttpPost, Route("register")]
        public IHttpActionResult Register(CustomerRegisterRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Username)
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.Name)
                || string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Username, password, ho ten va so dien thoai la bat buoc.");

            if (request.Password.Length < 6)
                return BadRequest("Mat khau phai co it nhat 6 ky tu.");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Kiểm tra username đã tồn tại chưa
                using (var check = new SqlCommand(
                    "SELECT COUNT(1) FROM Customers WHERE Username=@Username", conn))
                {
                    check.Parameters.AddWithValue("@Username", request.Username);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        return Conflict(); // 409
                }

                string id = "C" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                string hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                using (var cmd = new SqlCommand(@"
                    INSERT INTO Customers(Id, Username, Password, Name, Phone, Email, Address, Orders, Spent)
                    VALUES (@Id, @Username, @Password, @Name, @Phone, @Email, @Address, 0, 0)", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Username", request.Username);
                    cmd.Parameters.AddWithValue("@Password", hash);
                    cmd.Parameters.AddWithValue("@Name", request.Name);
                    cmd.Parameters.AddWithValue("@Phone", request.Phone);
                    cmd.Parameters.AddWithValue("@Email", request.Email ?? "");
                    cmd.Parameters.AddWithValue("@Address", request.Address ?? "");
                    cmd.ExecuteNonQuery();
                }

                return Ok(new { Id = id, request.Username, request.Name, message = "Dang ky thanh cong." });
            }
        }

        /// <summary>POST /api/portal/login</summary>
        [AllowAnonymous]
        [HttpPost, Route("login")]
        public IHttpActionResult Login(CustomerLoginRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Username)
                || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username va password la bat buoc.");

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT Id, Username, Password, Name, Phone, Email, Address
                FROM Customers
                WHERE Username=@Username", conn))
            {
                cmd.Parameters.AddWithValue("@Username", request.Username);
                conn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return Unauthorized();

                    string hash = rd["Password"].ToString();
                    if (!BCrypt.Net.BCrypt.Verify(request.Password, hash))
                        return Unauthorized();

                    string id = rd["Id"].ToString();
                    string name = rd["Name"].ToString();
                    string username = rd["Username"].ToString();

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, "customer"),
                        new Claim("CustomerId",    id),
                        new Claim("CustomerName",  name)
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                                    ConfigurationManager.AppSettings["JwtKey"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: "TechStore",
                        audience: "TechStore",
                        claims: claims,
                        expires: DateTime.Now.AddHours(8),
                        signingCredentials: creds);

                    return Ok(new
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        Id = id,
                        Username = username,
                        Name = name,
                        Role = "customer"
                    });
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // PRODUCTS (xem công khai, không cần token)
        // ─────────────────────────────────────────────────────────

        /// <summary>GET /api/portal/products</summary>
        [AllowAnonymous]
        [HttpGet, Route("products")]
        public IHttpActionResult GetProducts()
        {
            var items = new List<ProductDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT Id, Name, Category, Price, Stock, Brand, Description, Emoji
                FROM Products
                WHERE Stock > 0
                ORDER BY Name", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        items.Add(new ProductDto
                        {
                            Id = rd["Id"].ToString(),
                            Name = rd["Name"].ToString(),
                            Category = rd["Category"].ToString(),
                            Price = Convert.ToInt32(rd["Price"]),
                            Stock = Convert.ToInt32(rd["Stock"]),
                            Brand = rd["Brand"].ToString(),
                            Desc = rd["Description"].ToString(),
                            Emoji = rd["Emoji"].ToString()
                            // Cost không trả về cho user
                        });
                    }
                }
            }
            return Ok(items);
        }

        /// <summary>GET /api/portal/products/{id}</summary>
        /// <summary>GET /api/portal/products?name=&category=&minPrice=&maxPrice=</summary>
        [AllowAnonymous]
        [HttpGet, Route("products")]
        public IHttpActionResult GetProducts(
            string name = null,
            string category = null,
            int? minPrice = null,
            int? maxPrice = null)
        {
            var sql = new System.Text.StringBuilder(@"
        SELECT p.Id, p.Name, c.Name AS Category,
               p.Price, p.Stock, p.Brand, p.Description, p.Emoji
        FROM Products p
        LEFT JOIN Categories c ON c.Id = p.CategoryId
        WHERE p.Stock > 0");

            var items = new List<ProductDto>();
            using (var conn = new SqlConnection(_connStr))
            {
                var cmd = new SqlCommand { Connection = conn };

                if (!string.IsNullOrWhiteSpace(name))
                {
                    sql.Append(" AND p.Name LIKE @Name");
                    cmd.Parameters.AddWithValue("@Name", $"%{name.Trim()}%");
                }
                if (!string.IsNullOrWhiteSpace(category))
                {
                    sql.Append(" AND c.Name LIKE @Cat");
                    cmd.Parameters.AddWithValue("@Cat", $"%{category.Trim()}%");
                }
                if (minPrice.HasValue)
                {
                    sql.Append(" AND p.Price >= @Min");
                    cmd.Parameters.AddWithValue("@Min", minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    sql.Append(" AND p.Price <= @Max");
                    cmd.Parameters.AddWithValue("@Max", maxPrice.Value);
                }
                sql.Append(" ORDER BY p.Name");
                cmd.CommandText = sql.ToString();

                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        items.Add(new ProductDto
                        {
                            Id = rd["Id"].ToString(),
                            Name = rd["Name"].ToString(),
                            Category = rd["Category"] == DBNull.Value ? "" : rd["Category"].ToString(),
                            Price = Convert.ToInt32(rd["Price"]),
                            Stock = Convert.ToInt32(rd["Stock"]),
                            Brand = rd["Brand"].ToString(),
                            Desc = rd["Description"].ToString(),
                            Emoji = rd["Emoji"].ToString()
                        });
                    }
                }
            }
            return Ok(items);
        }

        // ─────────────────────────────────────────────────────────
        // ORDERS (cần token customer)
        // ─────────────────────────────────────────────────────────

        /// <summary>POST /api/portal/orders — khách đặt hàng</summary>
        [Authorize(Roles = "customer")]
        [HttpPost, Route("orders")]
        public IHttpActionResult PlaceOrder(CustomerPortalOrderRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductId) || request.Qty <= 0)
                return BadRequest("ProductId va so luong la bat buoc.");

            string customerId = GetClaimValue("CustomerId");
            string customerName = GetClaimValue("CustomerName");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Lấy thông tin sản phẩm
                int price; string productName;
                using (var cmd = new SqlCommand(
                    "SELECT Name, Price, Stock FROM Products WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", request.ProductId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return BadRequest("San pham khong ton tai.");
                        int stock = Convert.ToInt32(rd["Stock"]);
                        if (stock < request.Qty)
                            return BadRequest($"Khong du hang. Con lai: {stock}.");
                        price = Convert.ToInt32(rd["Price"]);
                        productName = rd["Name"].ToString();
                    }
                }

                int total = price * request.Qty;
                string orderId = "ORD" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO Orders(Id, CustomerId, ProductId, Customer, Product, Qty, Total, Status, OrderDate)
                            VALUES (@Id, @CustomerId, @ProductId, @Customer, @Product, @Qty, @Total, N'Chờ xử lý', GETDATE())", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", orderId);
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            cmd.Parameters.AddWithValue("@ProductId", request.ProductId);
                            cmd.Parameters.AddWithValue("@Customer", customerName);
                            cmd.Parameters.AddWithValue("@Product", productName);
                            cmd.Parameters.AddWithValue("@Qty", request.Qty);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.ExecuteNonQuery();
                        }

                        // Trừ tồn kho
                        using (var cmd = new SqlCommand(
                            "UPDATE Products SET Stock=Stock-@Qty WHERE Id=@Id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Qty", request.Qty);
                            cmd.Parameters.AddWithValue("@Id", request.ProductId);
                            cmd.ExecuteNonQuery();
                        }

                        // Cập nhật thống kê customer
                        using (var cmd = new SqlCommand(@"
                            UPDATE Customers SET Orders=Orders+1, Spent=Spent+@Total
                            WHERE Id=@CustomerId", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }

                return Ok(new
                {
                    OrderId = orderId,
                    Product = productName,
                    Qty = request.Qty,
                    Total = total,
                    Status = "Chờ xử lý",
                    message = "Dat hang thanh cong."
                });
            }
        }

        /// <summary>GET /api/portal/orders — xem đơn hàng của mình</summary>
        [Authorize(Roles = "customer")]
        [HttpGet, Route("orders")]
        public IHttpActionResult GetMyOrders()
        {
            string customerId = GetClaimValue("CustomerId");
            var items = new List<object>();

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT o.Id, p.Name AS Product, o.Qty, o.Total, o.Status, o.OrderDate
                FROM Orders o
                LEFT JOIN Products p ON p.Id = o.ProductId
                WHERE o.CustomerId = @CustomerId
                ORDER BY o.OrderDate DESC", conn))
            {
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        items.Add(new
                        {
                            Id = rd["Id"].ToString(),
                            Product = rd["Product"].ToString(),
                            Qty = Convert.ToInt32(rd["Qty"]),
                            Total = Convert.ToInt32(rd["Total"]),
                            Status = rd["Status"].ToString(),
                            Date = Convert.ToDateTime(rd["OrderDate"]).ToString("yyyy-MM-dd")
                        });
                    }
                }
            }
            return Ok(items);
        }

        // ─────────────────────────────────────────────────────────
        // PROFILE
        // ─────────────────────────────────────────────────────────

        /// <summary>GET /api/portal/orders/{id} — xem chi tiết 1 đơn hàng của mình</summary>
        [Authorize(Roles = "customer")]
        [HttpGet, Route("orders/{id}")]
        public IHttpActionResult GetMyOrder(string id)
        {
            string customerId = GetClaimValue("CustomerId");

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
        SELECT o.Id, o.ProductId, p.Name AS Product, p.Brand, p.Emoji,
               o.Qty, o.Total, o.Status, o.OrderDate
        FROM Orders o
        LEFT JOIN Products p ON p.Id = o.ProductId
        WHERE o.Id = @Id AND o.CustomerId = @CustomerId", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return NotFound();
                    return Ok(new
                    {
                        Id = rd["Id"].ToString(),
                        ProductId = rd["ProductId"].ToString(),
                        Product = rd["Product"].ToString(),
                        Brand = rd["Brand"].ToString(),
                        Emoji = rd["Emoji"].ToString(),
                        Qty = Convert.ToInt32(rd["Qty"]),
                        Total = Convert.ToInt32(rd["Total"]),
                        Status = rd["Status"].ToString(),
                        Date = Convert.ToDateTime(rd["OrderDate"]).ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
        }

        /// <summary>DELETE /api/portal/orders/{id} — khách hủy đơn (chỉ được hủy khi "Chờ xử lý")</summary>
        [Authorize(Roles = "customer")]
        [HttpDelete, Route("orders/{id}")]
        public IHttpActionResult CancelMyOrder(string id)
        {
            string customerId = GetClaimValue("CustomerId");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Lấy thông tin đơn — phải là của chính khách hàng này
                string status; string productId; int qty; int total;
                using (var cmd = new SqlCommand(@"
                SELECT Status, ProductId, Qty, Total
                FROM Orders
                WHERE Id=@Id AND CustomerId=@CustomerId", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return NotFound();
                        status = rd["Status"].ToString();
                        productId = rd["ProductId"].ToString();
                        qty = Convert.ToInt32(rd["Qty"]);
                        total = Convert.ToInt32(rd["Total"]);
                    }
                }

                // Chỉ cho hủy khi đang "Chờ xử lý"
                if (status != "Chờ xử lý")
                    return BadRequest($"Không thể hủy đơn ở trạng thái '{status}'.");

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Cập nhật trạng thái đơn → Đã hủy
                        using (var cmd = new SqlCommand(
                            "UPDATE Orders SET Status=N'Đã hủy' WHERE Id=@Id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Hoàn lại tồn kho
                        using (var cmd = new SqlCommand(
                            "UPDATE Products SET Stock=Stock+@Qty WHERE Id=@ProductId", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@ProductId", productId);
                            cmd.ExecuteNonQuery();
                        }

                        // Trừ lại thống kê customer
                        using (var cmd = new SqlCommand(@"
                    UPDATE Customers
                    SET Orders = Orders - 1,
                        Spent  = Spent  - @Total
                    WHERE Id = @CustomerId", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }

            return Ok(new { id, status = "Đã hủy", message = "Hủy đơn thành công." });
        }

        /// <summary>GET /api/portal/profile</summary>
        [Authorize(Roles = "customer")]
        [HttpGet, Route("profile")]
        public IHttpActionResult GetProfile()
        {
            string customerId = GetClaimValue("CustomerId");

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT Id, Username, Name, Phone, Email, Address, Orders, Spent
                FROM Customers WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return NotFound();
                    return Ok(new
                    {
                        Id = rd["Id"].ToString(),
                        Username = rd["Username"].ToString(),
                        Name = rd["Name"].ToString(),
                        Phone = rd["Phone"].ToString(),
                        Email = rd["Email"].ToString(),
                        Address = rd["Address"].ToString(),
                        Orders = Convert.ToInt32(rd["Orders"]),
                        Spent = Convert.ToInt32(rd["Spent"])
                    });
                }
            }
        }

        /// <summary>PUT /api/portal/profile</summary>
        [Authorize(Roles = "customer")]
        [HttpPut, Route("profile")]
        public IHttpActionResult UpdateProfile(UpdateProfileRequest request)
        {
            if (request == null) return BadRequest("Du lieu khong hop le.");
            string customerId = GetClaimValue("CustomerId");

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                UPDATE Customers
                SET Name=@Name, Phone=@Phone, Email=@Email, Address=@Address
                WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                cmd.Parameters.AddWithValue("@Name", request.Name ?? "");
                cmd.Parameters.AddWithValue("@Phone", request.Phone ?? "");
                cmd.Parameters.AddWithValue("@Email", request.Email ?? "");
                cmd.Parameters.AddWithValue("@Address", request.Address ?? "");
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok(new { message = "Cap nhat thanh cong." });
        }

        /// <summary>POST /api/portal/change-password</summary>
        [Authorize(Roles = "customer")]
        [HttpPost, Route("change-password")]
        public IHttpActionResult ChangePassword(ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Mat khau moi la bat buoc.");
            if (request.NewPassword.Length < 6)
                return BadRequest("Mat khau moi phai co it nhat 6 ky tu.");

            string customerId = GetClaimValue("CustomerId");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                string currentHash;
                using (var cmd = new SqlCommand(
                    "SELECT Password FROM Customers WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    var result = cmd.ExecuteScalar();
                    if (result == null) return NotFound();
                    currentHash = result.ToString();
                }

                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword ?? "", currentHash))
                    return BadRequest("Mat khau hien tai khong dung.");

                string newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                using (var cmd = new SqlCommand(
                    "UPDATE Customers SET Password=@Password WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", customerId);
                    cmd.Parameters.AddWithValue("@Password", newHash);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { message = "Doi mat khau thanh cong." });
        }

        // ─────────────────────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────────────────────
        private string GetClaimValue(string type)
        {
            var identity = User.Identity as ClaimsIdentity;
            return identity?.FindFirst(type)?.Value ?? "";
        }
    }
}
