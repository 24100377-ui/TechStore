using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;


namespace WebApplication1.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // ── POST /api/auth/login ──────────────────────────────────────────────

        [HttpPost, Route("login")]
        public IHttpActionResult Login(LoginRequest request)
        {
            if (request == null) return BadRequest("Du lieu dang nhap khong hop le.");

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"
                SELECT Id, Username, Password, Fullname, Role, Status, CreatedAt, LastLogin
                FROM Users WHERE Username=@Username", conn))
            {
                cmd.Parameters.AddWithValue("@Username", request.Username ?? "");
                conn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return Unauthorized();

                    string hash = rd["Password"].ToString();
                    if (!BCrypt.Net.BCrypt.Verify(request.Password ?? "", hash))
                        return Unauthorized();

                    var user = new UserAccountDto
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

                    if (user.Status != "active")
                        return Content(HttpStatusCode.Forbidden, "Tai khoan da bi khoa.");

                    rd.Close();

                    using (var upd = new SqlCommand(
                        "UPDATE Users SET LastLogin=GETDATE() WHERE Id=@Id", conn))
                    {
                        upd.Parameters.AddWithValue("@Id", user.Id);
                        upd.ExecuteNonQuery();
                    }

                    return Ok(new { User = user, Token = BuildToken(user.Username, user.Role, user.Id) });
                }
            }
        }

        // ── POST /api/auth/forgot-password ───────────────────────────────────
        // Body: { "username": "...", "email": "..." }
        // Tạo token reset, lưu DB, gửi link qua email thật.

        [HttpPost, Route("forgot-password")]
        public IHttpActionResult ForgotPassword(ForgotPasswordRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Username)
                || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Username va Email la bat buoc.");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Tìm user khớp cả username + email (bảo mật hơn)
                string userId = null;
                using (var cmd = new SqlCommand(
                    "SELECT Id FROM Users WHERE Username=@Username AND Email=@Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", request.Username);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    var res = cmd.ExecuteScalar();
                    if (res != null) userId = res.ToString();
                }

                // Luôn trả thông báo trung lập – không tiết lộ user có tồn tại hay không
                if (userId == null)
                    return Ok(new { message = "Neu thong tin hop le, email dat lai mat khau da duoc gui." });

                // Tạo token reset ngẫu nhiên, hết hạn sau 30 phút
                string token = Guid.NewGuid().ToString("N");
                DateTime expiry = DateTime.UtcNow.AddMinutes(30);

                using (var cmd = new SqlCommand(@"
                    UPDATE Users
                    SET ResetToken=@Token, ResetTokenExpiry=@Expiry
                    WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    cmd.Parameters.AddWithValue("@Expiry", expiry);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }

                // Gửi email
                try
                {
                    string frontendUrl = ConfigurationManager.AppSettings["FrontendUrl"]
                                         ?? "http://localhost:3000";
                    string link = $"{frontendUrl}/reset-password?token={token}";

                    SendEmail(
                        to: request.Email,
                        subject: "[TechStore] Dat lai mat khau",
                        body: $"Xin chao {request.Username},\n\n" +
                                 $"Nhan vao link duoi de dat lai mat khau (het han sau 30 phut):\n{link}\n\n" +
                                 "Neu ban khong yeu cau, hay bo qua email nay.");
                }
                catch
                {
                    // Ghi log nhưng không để lỗi SMTP làm vỡ response
                }

                return Ok(new { message = "Neu thong tin hop le, email dat lai mat khau da duoc gui." });
            }
        }

        // ── POST /api/auth/reset-password ────────────────────────────────────
        // Body: { "token": "...", "newPassword": "..." }

        [HttpPost, Route("reset-password")]
        public IHttpActionResult ResetPassword([FromBody] dynamic body)
        {
            string token = body?.token?.ToString();
            string newPassword = body?.newPassword?.ToString();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest("Token va mat khau moi la bat buoc.");
            if (newPassword.Length < 6)
                return BadRequest("Mat khau phai co it nhat 6 ky tu.");

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                string userId = null;
                using (var cmd = new SqlCommand(@"
                    SELECT Id FROM Users
                    WHERE ResetToken=@Token AND ResetTokenExpiry > GETUTCDATE()", conn))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    var res = cmd.ExecuteScalar();
                    if (res == null) return BadRequest("Token khong hop le hoac da het han.");
                    userId = res.ToString();
                }

                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                using (var cmd = new SqlCommand(@"
                    UPDATE Users
                    SET Password=@Password, ResetToken=NULL, ResetTokenExpiry=NULL
                    WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Password", hash);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Dat lai mat khau thanh cong." });
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static string BuildToken(string username, string role, string userId)
        {
            var key = new SymmetricSecurityKey(
                             Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId)
            };

            var token = new JwtSecurityToken(
                issuer: "TechStore",
                audience: "TechStore",
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static void SendEmail(string to, string subject, string body)
        {
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPass"];
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? user;

            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(user, pass);

                var msg = new MailMessage(from, to, subject, body) { IsBodyHtml = false };
                client.Send(msg);
            }
        }
        [HttpGet]
        [Route("hash")]
        public IHttpActionResult Hash()
        {
            return Ok(BCrypt.Net.BCrypt.HashPassword("admin123"));
        }
    }
}