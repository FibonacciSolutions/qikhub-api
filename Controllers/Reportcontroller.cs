using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QikHubAPI.Data;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
        {
            var topProducts = await _context.Products
                .Where(p => p.AdminApproved == true && p.IsActive == true)
                .Select(p => new
                {
                    p.Id,
                    Title = p.Title,
                    p.Price,
                    TotalStock = p.Variants.Sum(v => v.Stock),
                    p.CreatedAt,
                    SellerName = p.Seller != null ? p.Seller.BusinessName : "Unknown"
                })
                .OrderByDescending(p => p.Price)
                .Take(limit)
                .ToListAsync();

            return Ok(topProducts);
        }

        [HttpGet("stock-report")]
        public async Task<IActionResult> GetStockReport([FromQuery] int? categoryId, [FromQuery] int? brandId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            var stockReport = await query
                .Select(p => new
                {
                    p.Id,
                    ProductTitle = p.Title,
                    CategoryName = p.Category != null ? p.Category.Title : "Unknown",
                    BrandName = p.Brand != null ? p.Brand.Title : "Unknown",
                    TotalStock = p.Variants.Sum(v => v.Stock),
                    LowStock = p.Variants.Any(v => v.Stock <= v.MinimumReorderLevel),
                    Variants = p.Variants.Select(v => new
                    {
                        v.Color,
                        v.Size,
                        v.SKU,
                        v.Stock,
                        v.MinimumReorderLevel
                    })
                })
                .ToListAsync();

            return Ok(stockReport);
        }
    }
}