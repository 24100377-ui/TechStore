using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// CRUD danh mục sản phẩm – chỉ Admin mới được tạo / sửa / xoá.
    /// GET danh sách public (AllowAnonymous).
    /// </summary>
    [RoutePrefix("api/categories")]
    public class CategoriesController : ApiController
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // GET /api/categories
        [AllowAnonymous]
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var list = new List<CategoryDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Name, Slug FROM Categories ORDER BY Name", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read())
                        list.Add(Read(rd));
            }
            return Ok(list);
        }

        // GET /api/categories/5
        [AllowAnonymous]
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Name, Slug FROM Categories WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return NotFound();
                    return Ok(Read(rd));
                }
            }
        }

        // POST /api/categories  [Admin]
        [Authorize(Roles = "Admin")]
        [HttpPost, Route("")]
        public IHttpActionResult Create(CategoryDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Ten danh muc la bat buoc.");

            if (string.IsNullOrWhiteSpace(dto.Slug))
                dto.Slug = Slugify(dto.Name);

            int newId;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                INSERT INTO Categories(Name, Slug) VALUES (@Name, @Slug);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Slug", dto.Slug);
                conn.Open();
                newId = (int)cmd.ExecuteScalar();
            }

            dto.Id = newId;
            return Ok(dto);
        }

        // PUT /api/categories/5  [Admin]
        [Authorize(Roles = "Admin")]
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, CategoryDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Ten danh muc la bat buoc.");

            if (string.IsNullOrWhiteSpace(dto.Slug))
                dto.Slug = Slugify(dto.Name);

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "UPDATE Categories SET Name=@Name, Slug=@Slug WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Slug", dto.Slug);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }

            dto.Id = id;
            return Ok(dto);
        }

        // DELETE /api/categories/5  [Admin]
        [Authorize(Roles = "Admin")]
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Kiểm tra còn sản phẩm dùng category này không
                using (var check = new SqlCommand(
                    "SELECT COUNT(1) FROM Products WHERE CategoryId=@Id", conn))
                {
                    check.Parameters.AddWithValue("@Id", id);
                    int count = (int)check.ExecuteScalar();
                    if (count > 0)
                        return BadRequest($"Khong the xoa: con {count} san pham dang dung danh muc nay.");
                }

                using (var cmd = new SqlCommand(
                    "DELETE FROM Categories WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    if (cmd.ExecuteNonQuery() == 0) return NotFound();
                }
            }
            return Ok(new { message = "Da xoa danh muc." });
        }

        private static CategoryDto Read(SqlDataReader rd) => new CategoryDto
        {
            Id = Convert.ToInt32(rd["Id"]),
            Name = rd["Name"].ToString(),
            Slug = rd["Slug"].ToString()
        };

        private static string Slugify(string name)
        {
            return name.Trim().ToLower()
                       .Replace(" ", "-")
                       .Replace("đ", "d");
        }
    }
}