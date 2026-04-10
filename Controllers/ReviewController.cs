using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QikHubAPI.Data;
using QikHubAPI.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User != null ? r.User.FullName : "Anonymous"
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return Ok(new { reviews, averageRating, totalReviews = reviews.Count });
        }

        [HttpPost("add")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddReview([FromBody] AddReviewDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Check if user already reviewed this product
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == userId);

            if (existingReview != null)
            {
                return BadRequest(new { message = "You have already reviewed this product" });
            }

            var review = new Review
            {
                ProductId = request.ProductId,
                UserId = userId,
                Rating = request.Rating,
                Title = request.Title,
                Comment = request.Comment,
                IsApproved = true, // Auto-approve for now
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully", reviewId = review.Id });
        }

        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReviews()
        {
            var pendingReviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => !r.IsApproved)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User != null ? r.User.FullName : "Anonymous",
                    ProductName = r.Product != null ? r.Product.Title : "Unknown"
                })
                .ToListAsync();

            return Ok(pendingReviews);
        }
        [HttpGet("service/{serviceId}")]
        public async Task<IActionResult> GetServiceReviews(int serviceId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ServiceId == serviceId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User != null ? r.User.FullName : "Anonymous"
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return Ok(new { reviews, averageRating, totalReviews = reviews.Count });
        }

        [HttpPost("add-service")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddServiceReview([FromBody] AddServiceReviewDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ServiceId == request.ServiceId && r.UserId == userId);

            if (existingReview != null)
            {
                return BadRequest(new { message = "You have already reviewed this service" });
            }

            var review = new Review
            {
                ServiceId = request.ServiceId,
                UserId = userId,
                Rating = request.Rating,
                Title = request.Title,
                Comment = request.Comment,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully" });
        }

        public class AddServiceReviewDto
        {
            public int ServiceId { get; set; }
            public int Rating { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Comment { get; set; } = string.Empty;
        }

        [HttpPost("admin/approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReview(int id, [FromBody] ApproveReviewDto request)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            review.IsApproved = request.Approve;
            await _context.SaveChangesAsync();

            return Ok(new { message = request.Approve ? "Review approved" : "Review rejected" });
        }
    }

    public class AddReviewDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }

    public class ApproveReviewDto
    {
        public bool Approve { get; set; }
    }
}