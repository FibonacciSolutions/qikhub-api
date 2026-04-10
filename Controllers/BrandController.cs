using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QikHubAPI.Data;
using QikHubAPI.Models;
using System;
using System.Threading.Tasks;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllBrands()
        {
            var brands = await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return Ok(brands);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            return Ok(brand);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto request)
        {
            string slug = request.Title.ToLower().Replace(" ", "-").Replace("?", "").Replace("/", "-");

            var brand = new Brand
            {
                Title = request.Title,
                Slug = slug,
                Description = request.Description ?? "",
                ThumbImage = request.ThumbImage ?? "",
                BannerImage = request.BannerImage ?? "",
                MetaTitle = request.MetaTitle ?? request.Title,
                MetaKeywords = request.MetaKeywords ?? "",
                MetaDescription = request.MetaDescription ?? "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand created successfully", brandId = brand.Id, brand });
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] UpdateBrandDto request)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                brand.Title = request.Title;
                brand.Slug = request.Title.ToLower().Replace(" ", "-").Replace("?", "").Replace("/", "-");
            }

            if (!string.IsNullOrEmpty(request.Description))
                brand.Description = request.Description;

            if (!string.IsNullOrEmpty(request.ThumbImage))
                brand.ThumbImage = request.ThumbImage;

            if (!string.IsNullOrEmpty(request.BannerImage))
                brand.BannerImage = request.BannerImage;

            if (request.IsActive.HasValue)
                brand.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand updated successfully", brand });
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Brand not found" });
            }

            // Check if has products
            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Cannot delete brand with products" });
            }

            brand.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand deleted successfully" });
        }
    }

    public class CreateBrandDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbImage { get; set; }
        public string? BannerImage { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaKeywords { get; set; }
        public string? MetaDescription { get; set; }
    }

    public class UpdateBrandDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ThumbImage { get; set; }
        public string? BannerImage { get; set; }
        public bool? IsActive { get; set; }
    }
}