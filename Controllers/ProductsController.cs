using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QikHubAPI.Data;
using QikHubAPI.Models;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllApprovedProducts()
        {
            var products = await _context.Products
                .Where(p => p.AdminApproved == true && p.IsActive == true)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    p.Price,
                    p.ThumbImage,
                    p.ProductType
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Price,
                    p.AdminApproved,
                    p.CreatedAt,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Title : "Unknown",
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.Title : "Unknown"
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Ok(products);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
        {
            try
            {
                var seller = await _context.Sellers.FirstOrDefaultAsync();
                if (seller == null)
                {
                    seller = new Seller
                    {
                        UserId = 1,
                        BusinessName = "Admin Store",
                        TaxId = "ADMIN123",
                        BankAccount = "AdminBank",
                        CommissionRate = 10,
                        Status = "Approved",
                        ApprovedAt = DateTime.UtcNow
                    };
                    _context.Sellers.Add(seller);
                    await _context.SaveChangesAsync();
                }

                var product = new Product
                {
                    SellerId = seller.Id,
                    CategoryId = request.CategoryId,
                    BrandId = request.BrandId,
                    Title = request.Title,
                    Slug = request.Title.ToLower().Replace(" ", "-"),
                    Description = request.Description ?? "",
                    ThumbImage = request.ThumbImage ?? "",
                    Price = request.Price,
                    ProductType = "Physical",
                    AdminApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (request.Stock > 0)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        SKU = $"SKU-{product.Id}",
                        Price = request.Price,
                        Stock = request.Stock,
                        IsActive = true
                    };
                    _context.ProductVariants.Add(variant);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Product created successfully", productId = product.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            product.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }
    }

    public class CreateProductDto
    {
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ThumbImage { get; set; } = string.Empty;
    }
}