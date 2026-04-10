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
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const decimal VAT_RATE = 0.13m;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var customerId = int.Parse(userIdClaim.Value);
            var customer = await _context.Users.FindAsync(customerId);

            if (customer == null)
            {
                return BadRequest(new { message = "Customer not found" });
            }

            var product = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            if (!product.AdminApproved)
            {
                return BadRequest(new { message = "Product is not available for purchase" });
            }

            // Calculate total stock from variants
            int totalStock = product.Variants.Sum(v => v.Stock);
            if (totalStock < request.Quantity)
            {
                return BadRequest(new { message = $"Only {totalStock} items available in stock" });
            }

            decimal subtotal = product.Price * request.Quantity;
            decimal vatAmount = subtotal * VAT_RATE;
            decimal totalAmount = subtotal + vatAmount;
            decimal commissionAmount = subtotal * (product.Seller?.CommissionRate ?? 10) / 100;
            decimal sellerEarnings = subtotal - commissionAmount;

            decimal creditUsed = 0;
            if (request.UseCredit && customer.CreditBalance > 0)
            {
                creditUsed = Math.Min(customer.CreditBalance, totalAmount);
                totalAmount -= creditUsed;
            }

            var order = new Order
            {
                CustomerId = customerId,
                SellerId = product.SellerId,
                DeliveryPersonId = null,
                TotalAmount = totalAmount,
                VatAmount = vatAmount,
                CommissionAmount = commissionAmount,
                SellerEarnings = sellerEarnings,
                Status = "Pending",
                AdminVerified = false,
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = null
            };

            _context.Orders.Add(order);

            // Update stock on first variant (for now)
            var firstVariant = product.Variants.FirstOrDefault();
            if (firstVariant != null)
            {
                firstVariant.Stock -= request.Quantity;
            }

            if (creditUsed > 0)
            {
                customer.CreditBalance -= creditUsed;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order created successfully. Awaiting admin verification.",
                orderId = order.Id,
                order = new
                {
                    order.Id,
                    order.TotalAmount,
                    order.VatAmount,
                    order.Status,
                    order.CreatedAt,
                    CreditUsed = creditUsed,
                    ProductTitle = product.Title,
                    Quantity = request.Quantity
                }
            });
        }

        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var customerId = int.Parse(userIdClaim.Value);

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Seller)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.VatAmount,
                    o.Status,
                    o.AdminVerified,
                    o.CreatedAt,
                    o.DeliveredAt,
                    SellerName = o.Seller != null ? o.Seller.BusinessName : "Unknown"
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }
    }

    public class CreateOrderDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool UseCredit { get; set; } = false;
    }
}