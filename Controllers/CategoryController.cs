using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QikHubAPI.Data;
using QikHubAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // Get all categories (public)
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.DisplayOrder)
                .ToListAsync();

            return Ok(categories);
        }

        // Get categories by level (public)
        [HttpGet("by-level/{level}")]
        public async Task<IActionResult> GetCategoriesByLevel(int level)
        {
            var categories = await _context.Categories
                .Where(c => c.Level == level && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return Ok(categories);
        }

        // Get subcategories (public)
        [HttpGet("subcategories/{parentId}")]
        public async Task<IActionResult> GetSubCategories(int parentId)
        {
            var subCategories = await _context.Categories
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return Ok(subCategories);
        }

        // Get category tree (public)
        [HttpGet("tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var allCategories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var tree = BuildCategoryTree(allCategories, null);
            return Ok(tree);
        }

        // Get single category (public)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            return Ok(category);
        }

        // Admin: Create category
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
        {
            // Generate slug
            string slug = request.Title.ToLower().Replace(" ", "-").Replace("?", "").Replace("/", "-");

            // Determine level
            int level = 1;
            if (request.ParentId.HasValue)
            {
                var parent = await _context.Categories.FindAsync(request.ParentId.Value);
                if (parent != null)
                {
                    level = parent.Level + 1;
                    if (level > 3)
                    {
                        return BadRequest(new { message = "Maximum 3 levels allowed" });
                    }
                }
            }

            var category = new Category
            {
                Title = request.Title,
                Slug = slug,
                Description = request.Description ?? "",
                ThumbImage = request.ThumbImage ?? "",
                BannerImage = request.BannerImage ?? "",
                MetaTitle = request.MetaTitle ?? request.Title,
                MetaKeywords = request.MetaKeywords ?? "",
                MetaDescription = request.MetaDescription ?? "",
                ParentId = request.ParentId,
                Level = level,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category created successfully",
                categoryId = category.Id,
                category
            });
        }

        // Admin: Update category
        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                category.Title = request.Title;
                category.Slug = request.Title.ToLower().Replace(" ", "-").Replace("?", "").Replace("/", "-");
            }

            if (!string.IsNullOrEmpty(request.Description))
                category.Description = request.Description;

            if (!string.IsNullOrEmpty(request.ThumbImage))
                category.ThumbImage = request.ThumbImage;

            if (!string.IsNullOrEmpty(request.BannerImage))
                category.BannerImage = request.BannerImage;

            if (!string.IsNullOrEmpty(request.MetaTitle))
                category.MetaTitle = request.MetaTitle;

            if (!string.IsNullOrEmpty(request.MetaKeywords))
                category.MetaKeywords = request.MetaKeywords;

            if (!string.IsNullOrEmpty(request.MetaDescription))
                category.MetaDescription = request.MetaDescription;

            if (request.DisplayOrder.HasValue)
                category.DisplayOrder = request.DisplayOrder.Value;

            if (request.IsActive.HasValue)
                category.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully", category });
        }

        // Admin: Delete category
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            // Check if has products
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Cannot delete category with products. Move or delete products first." });
            }

            // Soft delete - just mark inactive
            category.IsActive = false;

            // Also mark subcategories inactive
            foreach (var child in category.Children)
            {
                child.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully" });
        }

        private List<object> BuildCategoryTree(List<Category> allCategories, int? parentId)
        {
            return allCategories
                .Where(c => c.ParentId == parentId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Slug,
                    c.Level,
                    c.ThumbImage,
                    Children = BuildCategoryTree(allCategories, c.Id)
                })
                .Cast<object>()
                .ToList();
        }
    }

    public class CreateCategoryDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbImage { get; set; }
        public string? BannerImage { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaKeywords { get; set; }
        public string? MetaDescription { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ThumbImage { get; set; }
        public string? BannerImage { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaKeywords { get; set; }
        public string? MetaDescription { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}