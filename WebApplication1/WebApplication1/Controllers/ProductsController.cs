using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // ── GET /api/products?name=&category=&minPrice=&maxPrice= ─────────────
        // AllowAnonymous vì portal public cũng cần gọi.
        [AllowAnonymous]
        [HttpGet, Route("")]
        public IHttpActionResult GetAll(
            string name = null,
            string category = null,
            int? minPrice = null,
            int? maxPrice = null)
        {
            var sql = new StringBuilder(@"
                SELECT p.Id, p.Name, c.Name AS Category, p.CategoryId,
                       p.Cost, p.Price, p.Stock, p.Brand, p.Description, p.Emoji
                FROM Products p
                LEFT JOIN Categories c ON c.Id = p.CategoryId
                WHERE 1=1");
            var items = new List<ProductDto>();

            using (var conn = new SqlConnection(_connStr))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
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
                    sql.Append(" ORDER BY p.Id");
                    cmd.CommandText = sql.ToString();
                    conn.Open();
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read()) items.Add(ReadProduct(rd));
                }
            }
            return Ok(items);
        }

        // ── GET /api/products/{id} ────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet, Route("{id}")]
        public IHttpActionResult Get(string id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT p.Id, p.Name, c.Name AS Category, p.CategoryId,
                       p.Cost, p.Price, p.Stock, p.Brand, p.Description, p.Emoji
                FROM Products p
                LEFT JOIN Categories c ON c.Id = p.CategoryId
                WHERE p.Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return NotFound();
                    return Ok(ReadProduct(rd));
                }
            }
        }

        // ── POST /api/products  [Admin, Manager] ──────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, Route("")]
        public IHttpActionResult Create(ProductDto product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Name))
                return BadRequest("Ten san pham la bat buoc.");

            if (string.IsNullOrWhiteSpace(product.Id))
                product.Id = "P" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                INSERT INTO Products(Id, Name, CategoryId, Cost, Price, Stock, Brand, Description, Emoji)
                VALUES (@Id, @Name, @CategoryId, @Cost, @Price, @Stock, @Brand, @Description, @Emoji)", conn))
            {
                AddParams(cmd, product);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(product);
        }

        // ── PUT /api/products/{id}  [Admin, Manager] ──────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut, Route("{id}")]
        public IHttpActionResult Update(string id, ProductDto product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Name))
                return BadRequest("Ten san pham la bat buoc.");

            product.Id = id;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                UPDATE Products
                SET Name=@Name, CategoryId=@CategoryId, Cost=@Cost, Price=@Price,
                    Stock=@Stock, Brand=@Brand, Description=@Description, Emoji=@Emoji
                WHERE Id=@Id", conn))
            {
                AddParams(cmd, product);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok(product);
        }

        // ── DELETE /api/products/{id}  [Admin] ────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpDelete, Route("{id}")]
        public IHttpActionResult Delete(string id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM Products WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static ProductDto ReadProduct(SqlDataReader rd) => new ProductDto
        {
            Id = rd["Id"].ToString(),
            Name = rd["Name"].ToString(),
            Category = rd["Category"] == DBNull.Value ? "" : rd["Category"].ToString(),
            CategoryId = rd["CategoryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["CategoryId"]),
            Cost = rd.IsDBNull(rd.GetOrdinal("Cost")) ? 0 : Convert.ToInt32(rd["Cost"]),
            Price = Convert.ToInt32(rd["Price"]),
            Stock = Convert.ToInt32(rd["Stock"]),
            Brand = rd["Brand"].ToString(),
            Desc = rd["Description"].ToString(),
            Emoji = rd["Emoji"].ToString()
        };

        private static void AddParams(SqlCommand cmd, ProductDto p)
        {
            cmd.Parameters.AddWithValue("@Id", p.Id);
            cmd.Parameters.AddWithValue("@Name", p.Name ?? "");
            cmd.Parameters.AddWithValue("@CategoryId", p.CategoryId.HasValue ? (object)p.CategoryId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Cost", p.Cost);
            cmd.Parameters.AddWithValue("@Price", p.Price);
            cmd.Parameters.AddWithValue("@Stock", p.Stock);
            cmd.Parameters.AddWithValue("@Brand", p.Brand ?? "");
            cmd.Parameters.AddWithValue("@Description", p.Desc ?? "");
            cmd.Parameters.AddWithValue("@Emoji", p.Emoji ?? "");
        }
    }
}