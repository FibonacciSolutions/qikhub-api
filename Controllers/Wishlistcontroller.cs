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
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .Select(w => new
                {
                    w.Id,
                    w.ProductId,
                    Product = new
                    {
                        w.Product.Id,
                        w.Product.Title,
                        w.Product.Price,
                        w.Product.ThumbImage
                    },
                    w.CreatedAt
                })
                .ToListAsync();

            return Ok(wishlist);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == request.ProductId);

            if (exists)
            {
                return BadRequest(new { message = "Product already in wishlist" });
            }

            var wishlist = new Wishlist
            {
                UserId = userId,
                ProductId = request.ProductId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Added to wishlist", wishlistId = wishlist.Id });
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlist == null)
            {
                return NotFound(new { message = "Item not found in wishlist" });
            }

            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed from wishlist" });
        }
    }

    public class AddToWishlistDto
    {
        public int ProductId { get; set; }
    }
}