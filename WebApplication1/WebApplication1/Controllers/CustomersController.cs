using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [RoutePrefix("api/customers")]
    public class CustomersController : ApiController
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var items = new List<CustomerDto>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT Id, Name, Phone, Email, Address, Orders, Spent FROM Customers ORDER BY Id", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        items.Add(new CustomerDto
                        {
                            Id = rd["Id"].ToString(),
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
            return Ok(items);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(CustomerDto customer)
        {
            if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Phone)) return BadRequest("Ten va so dien thoai la bat buoc.");
            if (string.IsNullOrWhiteSpace(customer.Id)) customer.Id = NextId();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(@"INSERT INTO Customers(Id, Name, Phone, Email, Address, Orders, Spent)VALUES(@Id, @Name, @Phone, @Email, @Address, @Orders, @Spent)", conn))
            {
                cmd.Parameters.AddWithValue("@Id", customer.Id);
                cmd.Parameters.AddWithValue("@Name", customer.Name ?? "");
                cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? "");
                cmd.Parameters.AddWithValue("@Email", customer.Email ?? "");
                cmd.Parameters.AddWithValue("@Address", customer.Address ?? "");
                cmd.Parameters.AddWithValue("@Orders", customer.Orders);
                cmd.Parameters.AddWithValue("@Spent", customer.Spent);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(customer);
        }

        [HttpDelete, Route("{id}")]
        public IHttpActionResult Delete(string id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM Customers WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0) return NotFound();
            }
            return Ok();
        }

        private static string NextId()
        {
            return "C" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}