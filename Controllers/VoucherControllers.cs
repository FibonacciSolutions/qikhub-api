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
    public class VoucherController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VoucherController(AppDbContext context)
        {
            _context = context;
        }

        // ========== CUSTOMER: GET ALL ACTIVE VOUCHERS ==========
        [HttpGet("active")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetActiveVouchers()
        {
            var now = DateTime.UtcNow;

            var vouchers = await _context.Vouchers
                .Where(v => v.ExpiryDate > now)
                .Select(v => new
                {
                    v.Id,
                    v.Code,
                    v.DiscountType,
                    v.DiscountValue,
                    v.ExpiryDate,
                    DaysUntilExpiry = (v.ExpiryDate - now).Days
                })
                .ToListAsync();

            return Ok(vouchers);
        }

        // ========== CUSTOMER: VALIDATE VOUCHER ==========
        [HttpPost("validate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherDto request)
        {
            var now = DateTime.UtcNow;

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code.ToUpper() == request.Code.ToUpper());

            if (voucher == null)
            {
                return NotFound(new { message = "Invalid voucher code", isValid = false });
            }

            if (voucher.ExpiryDate < now)
            {
                return BadRequest(new { message = "Voucher has expired", isValid = false });
            }

            decimal discountAmount = 0;

            if (voucher.DiscountType == "Percentage")
            {
                discountAmount = request.OrderAmount * (voucher.DiscountValue / 100);
                if (discountAmount > request.OrderAmount)
                {
                    discountAmount = request.OrderAmount;
                }
            }
            else if (voucher.DiscountType == "Fixed")
            {
                discountAmount = voucher.DiscountValue;
                if (discountAmount > request.OrderAmount)
                {
                    discountAmount = request.OrderAmount;
                }
            }

            return Ok(new
            {
                isValid = true,
                voucher.Id,
                voucher.Code,
                voucher.DiscountType,
                voucher.DiscountValue,
                discountAmount,
                newTotal = request.OrderAmount - discountAmount,
                message = $"Voucher applied! You save {discountAmount}"
            });
        }

        // ========== ADMIN: CREATE NEW VOUCHER ==========
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherDto request)
        {
            // Check if code already exists
            var existingVoucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == request.Code.ToUpper());

            if (existingVoucher != null)
            {
                return BadRequest(new { message = "Voucher code already exists" });
            }

            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var adminId = adminIdClaim != null ? int.Parse(adminIdClaim.Value) : 0;

            var voucher = new Voucher
            {
                Code = request.Code.ToUpper(),
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                ExpiryDate = request.ExpiryDate,
                CreatedByAdminId = adminId
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Voucher created successfully",
                voucher = new
                {
                    voucher.Id,
                    voucher.Code,
                    voucher.DiscountType,
                    voucher.DiscountValue,
                    voucher.ExpiryDate
                }
            });
        }

        // ========== ADMIN: GET ALL VOUCHERS ==========
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllVouchers()
        {
            var now = DateTime.UtcNow;

            var vouchers = await _context.Vouchers
                .Select(v => new
                {
                    v.Id,
                    v.Code,
                    v.DiscountType,
                    v.DiscountValue,
                    v.ExpiryDate,
                    v.CreatedByAdminId,
                    IsExpired = v.ExpiryDate < now,
                    DaysUntilExpiry = (v.ExpiryDate - now).Days
                })
                .OrderByDescending(v => v.ExpiryDate)
                .ToListAsync();

            return Ok(new
            {
                TotalVouchers = vouchers.Count,
                ActiveVouchers = vouchers.Count(v => !v.IsExpired),
                ExpiredVouchers = vouchers.Count(v => v.IsExpired),
                Vouchers = vouchers
            });
        }

        // ========== ADMIN: DELETE VOUCHER ==========
        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);

            if (voucher == null)
            {
                return NotFound(new { message = "Voucher not found" });
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Voucher {voucher.Code} deleted successfully" });
        }

        // ========== ADMIN: UPDATE VOUCHER ==========
        [HttpPut("admin/update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] UpdateVoucherDto request)
        {
            var voucher = await _context.Vouchers.FindAsync(id);

            if (voucher == null)
            {
                return NotFound(new { message = "Voucher not found" });
            }

            if (!string.IsNullOrEmpty(request.Code))
                voucher.Code = request.Code.ToUpper();

            if (!string.IsNullOrEmpty(request.DiscountType))
                voucher.DiscountType = request.DiscountType;

            if (request.DiscountValue.HasValue)
                voucher.DiscountValue = request.DiscountValue.Value;

            if (request.ExpiryDate.HasValue)
                voucher.ExpiryDate = request.ExpiryDate.Value;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Voucher updated successfully",
                voucher
            });
        }
    }

    // ========== DTO CLASSES ==========

    public class ValidateVoucherDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
    }

    public class CreateVoucherDto
    {
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty; // Percentage or Fixed
        public decimal DiscountValue { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class UpdateVoucherDto
    {
        public string? Code { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
