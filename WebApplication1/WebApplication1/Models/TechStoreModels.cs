using System;

namespace WebApplication1.Models
{
    // ─── Products / Category ───────────────────────────────────────────────────
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class ProductDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }   // tên category (readonly join)
        public int? CategoryId { get; set; }   // FK sang bảng Categories
        public int Cost { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }
        public string Brand { get; set; }
        public string Desc { get; set; }
        public string Emoji { get; set; }
    }

    // ─── Customer ──────────────────────────────────────────────────────────────

    public class CustomerDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public int Orders { get; set; }
        public int Spent { get; set; }
    }

    // ─── Orders ────────────────────────────────────────────────────────────────

    public class OrderDto
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public int Qty { get; set; }
        public int Total { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
    }

    // ─── Cart ──────────────────────────────────────────────────────────────────

    public class CartItemDto
    {
        public int Id { get; set; }
        public string ProductId { get; set; }
        public string Product { get; set; }   // tên (readonly join)
        public int Price { get; set; }   // giá lúc thêm vào giỏ
        public int Qty { get; set; }
        public int Subtotal { get; set; }   // Price * Qty
        public string Emoji { get; set; }
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; }
        public int Qty { get; set; }
    }

    public class UpdateCartQtyRequest
    {
        public int Qty { get; set; }
    }

    // ─── Reviews / Ratings ─────────────────────────────────────────────────────

    public class ReviewDto
    {
        public int Id { get; set; }
        public string ProductId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int Rating { get; set; }   // 1-5
        public string Comment { get; set; }
        public string CreatedAt { get; set; }
    }

    public class CreateReviewRequest
    {
        public string ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    // ─── Auth ──────────────────────────────────────────────────────────────────

    public class CustomerRegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public class CustomerLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class CustomerPortalOrderRequest
    {
        public string ProductId { get; set; }
        public int Qty { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public class UserAccountDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Fullname { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public string LastLogin { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ToggleStatusRequest
    {
        public string Status { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; }
    }

    public class StockRequest
    {
        public string ProductId { get; set; }
        public string Action { get; set; }
        public int Qty { get; set; }
        public string Note { get; set; }
    }

    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string Time { get; set; }
        public string User { get; set; }
        public string Fullname { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
    }

    public class CreateActivityLogRequest
    {
        public string User { get; set; }
        public string Fullname { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
    }

    // ─── Search / Filter ───────────────────────────────────────────────────────

    public class ProductSearchParams
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
    }
}