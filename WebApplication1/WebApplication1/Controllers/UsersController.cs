using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using WebApplication1.Models;
using BCrypt.Net;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var items = new List<UserAccountDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT Id, Username, Password, Fullname, Role, Status, CreatedAt, LastLogin FROM Users ORDER BY Id", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read()) items.Add(ReadUser(rd));
                }
            }
            return Ok(items);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(UserAccountDto user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password)) return BadRequest("Username va password la bat buoc.");
            if (string.IsNullOrWhiteSpace(user.Id)) user.Id = NextId();
            if (string.IsNullOrWhiteSpace(user.Status)) user.Status = "active";
            if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "staff";
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"INSERT INTO Users(Id, Username, Password, Fullname, Role, Status, CreatedAt) VALUES (@Id, @Username, @Password, @Fullname, @Role, @Status, GETDATE())", conn))
            {
                AddUserParams(cmd, user);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Fullname,
                user.Role,
                user.Status
            });
        }

        [HttpPut, Route("{id}")]
        public IHttpActionResult Update(string id, UserAccountDto user)
        {
            if (user == null) return BadRequest("Du lieu khong hop le.");
            user.Id = id;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"UPDATE Users SET Username=@Username, Fullname=@Fullname, Role=@Role, Status=@Status WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", user.Id);
                cmd.Parameters.AddWithValue("@Username", user.Username ?? "");
                cmd.Parameters.AddWithValue("@Fullname", user.Fullname ?? "");
                cmd.Parameters.AddWithValue("@Role", user.Role ?? "staff");
                cmd.Parameters.AddWithValue("@Status", user.Status ?? "active");
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Fullname,
                user.Role,
                user.Status
            });
        }

        [HttpPost, Route("{id}/change-password")]
        public IHttpActionResult ChangePassword(string id, ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Mat khau moi la bat buoc.");

            if (request.NewPassword.Length < 6)
                return BadRequest("Mat khau moi phai co it nhat 6 ky tu.");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                string currentHash = null;

                using (var getCmd = new SqlCommand(
                    "SELECT Password FROM Users WHERE Id=@Id",
                    conn))
                {
                    getCmd.Parameters.AddWithValue("@Id", id);

                    var result = getCmd.ExecuteScalar();

                    if (result == null)
                        return NotFound();

                    currentHash = result.ToString();
                }

                bool validPassword =
                    BCrypt.Net.BCrypt.Verify(
                        request.OldPassword ?? "",
                        currentHash);

                if (!validPassword)
                    return BadRequest("Mat khau hien tai khong dung.");

                string newHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.NewPassword);

                using (var updateCmd = new SqlCommand(
                    "UPDATE Users SET Password=@Password WHERE Id=@Id",
                    conn))
                {
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    updateCmd.Parameters.AddWithValue("@Password", newHash);

                    updateCmd.ExecuteNonQuery();
                }
            }

            return Ok(new
            {
                id,
                message = "Doi mat khau thanh cong."
            });
        }

        [HttpPost, Route("{id}/toggle-status")]
        public IHttpActionResult ToggleStatus(string id, ToggleStatusRequest request)
        {
            string newStatus;
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                if (request != null && !string.IsNullOrWhiteSpace(request.Status))
                {
                    newStatus = request.Status;
                }
                else
                {
                    using (var get = new SqlCommand("SELECT Status FROM Users WHERE Id=@Id", conn))
                    {
                        get.Parameters.AddWithValue("@Id", id);
                        var current = get.ExecuteScalar();
                        if (current == null) return NotFound();
                        newStatus = current.ToString() == "active" ? "locked" : "active";
                    }
                }

                using (var update = new SqlCommand("UPDATE Users SET Status=@Status WHERE Id=@Id", conn))
                {
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@Status", newStatus);
                    if (update.ExecuteNonQuery() == 0) return NotFound();
                }
            }
            return Ok(new { id, status = newStatus });
        }

        private static string NextId()
        {

            return "U" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        private static UserAccountDto ReadUser(SqlDataReader rd)
        {
            return new UserAccountDto
            {
                Id = rd["Id"].ToString(),
                Username = rd["Username"].ToString(),


                Fullname = rd["Fullname"].ToString(),
                Role = rd["Role"].ToString(),
                Status = rd["Status"].ToString(),
                CreatedAt = Convert.ToDateTime(rd["CreatedAt"]).ToString("yyyy-MM-dd"),
                LastLogin = rd["LastLogin"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(rd["LastLogin"]).ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        private static void AddUserParams(SqlCommand cmd, UserAccountDto user)
        {
            cmd.Parameters.AddWithValue("@Id", user.Id);
            cmd.Parameters.AddWithValue("@Username", user.Username ?? "");

            string hashedPassword =
                BCrypt.Net.BCrypt.HashPassword(user.Password ?? "");

            cmd.Parameters.AddWithValue("@Password", hashedPassword);

            cmd.Parameters.AddWithValue("@Fullname", user.Fullname ?? "");
            cmd.Parameters.AddWithValue("@Role", user.Role ?? "staff");
            cmd.Parameters.AddWithValue("@Status", user.Status ?? "active");
        }
    }
}