using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using QikHubAPI.Data;
using QikHubAPI.Models;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            return Ok(new
            {
                users = new { totalCustomers },
                products = new { totalProducts },
                orders = new { totalOrders, pendingOrders, completedOrders },
                revenue = new { totalProductRevenue = totalRevenue, totalServiceRevenue = 0 }
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Seller)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.AdminVerified,
                    o.CreatedAt,
                    CustomerName = o.Customer != null ? o.Customer.FullName : "Unknown",
                    CustomerPhone = o.Customer != null ? o.Customer.Phone : "N/A",
                    CustomerEmail = o.Customer != null ? o.Customer.Email : "N/A"
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPost("orders/verify/{orderId}")]
        public async Task<IActionResult> VerifyOrder(int orderId, [FromBody] VerifyOrderRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            order.AdminVerified = request.Verify;
            order.Status = request.Verify ? "Verified" : "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new { message = request.Verify ? "Order verified" : "Order rejected" });
        }

        [HttpPost("orders/complete/{orderId}")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            order.Status = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order marked as completed" });
        }
    }

    public class VerifyOrderRequest
    {
        public bool Verify { get; set; }
    }
}