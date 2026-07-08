using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [RoutePrefix("api/activity")]
    public class ActivityController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var items = new List<ActivityLogDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"SELECT TOP 200 Id, CreatedAt, Username, Fullname, Role, Action, Detail FROM ActivityLogs ORDER BY CreatedAt DESC, Id DESC", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        items.Add(new ActivityLogDto
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            Time = Convert.ToDateTime(rd["CreatedAt"]).ToString("dd/MM/yyyy HH:mm:ss"),
                            User = rd["Username"].ToString(),
                            Fullname = rd["Fullname"].ToString(),
                            Role = rd["Role"].ToString(),
                            Action = rd["Action"].ToString(),
                            Detail = rd["Detail"].ToString()
                        });
                    }
                }
            }
            return Ok(items);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(CreateActivityLogRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Action))
                return BadRequest("Action la bat buoc.");

            var identity = User.Identity as ClaimsIdentity;
            var username = identity?.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var role = identity?.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var userId = identity?.FindFirst("UserId")?.Value ?? "";

            string fullname = "";
            if (!string.IsNullOrEmpty(userId))
            {
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand("SELECT Fullname FROM Users WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", userId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null) fullname = result.ToString();
                }
            }

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"INSERT INTO ActivityLogs(Username, Fullname, Role, Action, Detail, CreatedAt, UserId)
                VALUES (@Username, @Fullname, @Role, @Action, @Detail, GETDATE(), @UserId)", conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Fullname", fullname);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@Action", request.Action);
                cmd.Parameters.AddWithValue("@Detail", request.Detail ?? "");
                cmd.Parameters.AddWithValue("@UserId", string.IsNullOrEmpty(userId) ? (object)DBNull.Value : userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Da ghi nhat ky." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete, Route("")]
        public IHttpActionResult Clear()
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM ActivityLogs", conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Da xoa nhat ky." });
        }
    }
}