using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QikHubAPI.Data;
using QikHubAPI.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceBookingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const decimal VAT_RATE = 0.13m;

        public ServiceBookingsController(AppDbContext context)
        {
            _context = context;
        }

        // ========== CUSTOMER: CREATE NEW SERVICE BOOKING ==========
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
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

            var service = await _context.Services
                .Include(s => s.Provider)
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId);

            if (service == null)
            {
                return NotFound(new { message = "Service not found" });
            }

            if (!service.AdminApproved)
            {
                return BadRequest(new { message = "Service is not available for booking" });
            }

            if (service.Provider == null || service.Provider.Status != "Approved")
            {
                return BadRequest(new { message = "Service provider is not available" });
            }

            if (service.Provider.LicenseExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Service provider's license has expired" });
            }

            decimal subtotal = service.PricePerHour * request.Hours;
            decimal vatAmount = subtotal * VAT_RATE;
            decimal totalAmount = subtotal + vatAmount;

            if (request.BookingDate <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Booking date must be in the future" });
            }

            decimal creditUsed = 0;
            if (request.UseCredit && customer.CreditBalance > 0)
            {
                creditUsed = Math.Min(customer.CreditBalance, totalAmount);
                totalAmount -= creditUsed;
            }

            var booking = new ServiceBooking
            {
                CustomerId = customerId,
                ProviderId = service.ProviderId,
                ServiceId = service.Id,
                BookingDate = request.BookingDate,
                TotalAmount = totalAmount,
                VatAmount = vatAmount,
                Status = "Pending",
                AdminVerified = false
            };

            _context.ServiceBookings.Add(booking);

            if (creditUsed > 0)
            {
                customer.CreditBalance -= creditUsed;
            }

            var journalEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Cash/Bank",
                AccountCredit = "Service Revenue",
                Amount = subtotal,
                Narration = $"Service booking: {service.Name} for {request.Hours} hours",
                ServiceBookingId = booking.Id
            };
            _context.JournalEntries.Add(journalEntry);

            var vatEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Service Revenue",
                AccountCredit = "VAT Payable",
                Amount = vatAmount,
                Narration = $"VAT on service booking #{booking.Id}",
                ServiceBookingId = booking.Id
            };
            _context.JournalEntries.Add(vatEntry);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Service booking created successfully. Awaiting admin verification.",
                bookingId = booking.Id,
                booking = new
                {
                    booking.Id,
                    booking.BookingDate,
                    booking.TotalAmount,
                    booking.VatAmount,
                    booking.Status,
                    Hours = request.Hours,
                    ServiceName = service.Name,
                    ProviderName = service.Provider.LicenseNumber,
                    CreditUsed = creditUsed
                }
            });
        }

        // ========== CUSTOMER: GET MY BOOKINGS ==========
        [HttpGet("my-bookings")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var customerId = int.Parse(userIdClaim.Value);

            var bookings = await _context.ServiceBookings
                .Where(b => b.CustomerId == customerId)
                .Include(b => b.Service)
                .Include(b => b.Provider)
                .Select(b => new
                {
                    b.Id,
                    b.BookingDate,
                    b.TotalAmount,
                    b.VatAmount,
                    b.Status,
                    b.AdminVerified,
                    ServiceName = b.Service != null ? b.Service.Name : "Unknown",
                    ProviderName = b.Provider != null ? b.Provider.LicenseNumber : "Unknown",
                    PricePerHour = b.Service != null ? b.Service.PricePerHour : 0
                })
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Ok(bookings);
        }

        // ========== SERVICE PROVIDER: GET MY BOOKINGS ==========
        [HttpGet("my-provider-bookings")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> GetMyProviderBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var provider = await _context.ServicePros.FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return BadRequest(new { message = "Service provider profile not found" });
            }

            var bookings = await _context.ServiceBookings
                .Where(b => b.ProviderId == provider.Id)
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Select(b => new
                {
                    b.Id,
                    b.BookingDate,
                    b.TotalAmount,
                    b.VatAmount,
                    b.Status,
                    b.AdminVerified,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Unknown",
                    CustomerPhone = b.Customer != null ? b.Customer.Phone : "Unknown",
                    ServiceName = b.Service != null ? b.Service.Name : "Unknown"
                })
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Ok(bookings);
        }

        // ========== ADMIN: GET ALL BOOKINGS ==========
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAllBookings()
        {
            var bookings = await _context.ServiceBookings
                .Include(b => b.Customer)
                .Include(b => b.Provider)
                .Include(b => b.Service)
                .Select(b => new
                {
                    b.Id,
                    b.BookingDate,
                    b.TotalAmount,
                    b.VatAmount,
                    b.Status,
                    b.AdminVerified,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Unknown",
                    ProviderName = b.Provider != null ? b.Provider.LicenseNumber : "Unknown",
                    ServiceName = b.Service != null ? b.Service.Name : "Unknown"
                })
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Ok(bookings);
        }

        // ========== ADMIN: VERIFY BOOKING ==========
        [HttpPost("admin/verify/{bookingId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyBooking(int bookingId, [FromBody] VerifyBookingDto request)
        {
            var booking = await _context.ServiceBookings
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            booking.AdminVerified = request.Verify;

            if (request.Verify)
            {
                booking.Status = "Verified";
            }
            else
            {
                booking.Status = "Rejected";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = request.Verify ? "Booking verified successfully" : "Booking rejected",
                bookingId = booking.Id,
                booking.Status,
                booking.AdminVerified
            });
        }

        // ========== SERVICE PROVIDER: ACCEPT BOOKING ==========
        [HttpPut("provider/accept/{bookingId}")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var provider = await _context.ServicePros.FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return BadRequest(new { message = "Service provider profile not found" });
            }

            var booking = await _context.ServiceBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == provider.Id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status != "Verified")
            {
                return BadRequest(new { message = "Booking must be admin verified before acceptance" });
            }

            booking.Status = "Accepted";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking accepted successfully",
                bookingId = booking.Id,
                booking.Status
            });
        }

        // ========== SERVICE PROVIDER: MARK AS COMPLETED ==========
        [HttpPut("provider/complete/{bookingId}")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var provider = await _context.ServicePros.FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return BadRequest(new { message = "Service provider profile not found" });
            }

            var booking = await _context.ServiceBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == provider.Id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status != "Accepted")
            {
                return BadRequest(new { message = "Booking must be accepted before marking as completed" });
            }

            booking.Status = "CompletedByProvider";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Service marked as completed. Awaiting customer confirmation.",
                bookingId = booking.Id,
                booking.Status
            });
        }

        // ========== CUSTOMER: CONFIRM SERVICE COMPLETION ==========
        [HttpPost("customer/confirm-completion/{bookingId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ConfirmCompletion(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var customerId = int.Parse(userIdClaim.Value);

            var booking = await _context.ServiceBookings
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status != "CompletedByProvider")
            {
                return BadRequest(new { message = "Service has not been marked as completed by provider yet" });
            }

            booking.Status = "Completed";
            await _context.SaveChangesAsync();

            decimal subtotal = booking.TotalAmount - booking.VatAmount;
            decimal commissionAmount = subtotal * (booking.Provider?.CommissionRate ?? 15) / 100;
            decimal providerEarnings = subtotal - commissionAmount;

            return Ok(new
            {
                message = "Service completed successfully. Thank you for using QikHub!",
                bookingId = booking.Id,
                booking.Status,
                ProviderEarnings = providerEarnings,
                CommissionAmount = commissionAmount
            });
        }

        // ========== CUSTOMER: CANCEL BOOKING ==========
        [HttpPut("customer/cancel/{bookingId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var customerId = int.Parse(userIdClaim.Value);

            var booking = await _context.ServiceBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status == "Completed" || booking.Status == "CompletedByProvider")
            {
                return BadRequest(new { message = "Cannot cancel completed service" });
            }

            if (booking.BookingDate <= DateTime.UtcNow.AddHours(24))
            {
                return BadRequest(new { message = "Cannot cancel booking within 24 hours of service time" });
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking cancelled successfully",
                bookingId = booking.Id,
                booking.Status
            });
        }

        // ========== ADMIN: GET BOOKING STATISTICS ==========
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBookingStatistics()
        {
            var totalBookings = await _context.ServiceBookings.CountAsync();
            var pendingBookings = await _context.ServiceBookings.CountAsync(b => b.Status == "Pending");
            var completedBookings = await _context.ServiceBookings.CountAsync(b => b.Status == "Completed");
            var totalRevenue = await _context.ServiceBookings.SumAsync(b => b.TotalAmount);

            var last30Days = DateTime.UtcNow.AddDays(-30);
            var recentBookings = await _context.ServiceBookings
                .Where(b => b.BookingDate >= last30Days)
                .CountAsync();

            var next7Days = DateTime.UtcNow.AddDays(7);
            var upcomingBookings = await _context.ServiceBookings
                .Where(b => b.BookingDate >= DateTime.UtcNow && b.BookingDate <= next7Days && b.Status != "Completed" && b.Status != "Cancelled")
                .CountAsync();

            return Ok(new
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                CompletedBookings = completedBookings,
                TotalRevenue = totalRevenue,
                RecentBookingsLast30Days = recentBookings,
                UpcomingBookingsNext7Days = upcomingBookings
            });
        }

        // ========== ADMIN: FORCE DELETE BOOKING ==========
        [HttpDelete("admin/force-delete/{bookingId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminForceDeleteBooking(int bookingId)
        {
            var booking = await _context.ServiceBookings.FindAsync(bookingId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            _context.ServiceBookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking forcefully deleted by admin" });
        }

        // ========== GET AVAILABLE TIME SLOTS ==========
        [HttpGet("available-slots/{providerId}")]
        public async Task<IActionResult> GetAvailableSlots(int providerId, [FromQuery] DateTime date)
        {
            var existingBookings = await _context.ServiceBookings
                .Where(b => b.ProviderId == providerId &&
                       b.BookingDate.Date == date.Date &&
                       b.Status != "Cancelled" &&
                       b.Status != "Rejected")
                .Select(b => b.BookingDate)
                .ToListAsync();

            var allSlots = new List<DateTime>();
            var startTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0);
            var endTime = new DateTime(date.Year, date.Month, date.Day, 18, 0, 0);

            for (var time = startTime; time <= endTime; time = time.AddHours(1))
            {
                allSlots.Add(time);
            }

            var availableSlots = allSlots
                .Where(slot => !existingBookings.Any(b => b.Hour == slot.Hour && b.Date == slot.Date))
                .ToList();

            return Ok(new
            {
                Date = date,
                AvailableSlots = availableSlots,
                BookedSlots = existingBookings
            });
        }
    }

    // ========== DTO CLASSES ==========

    public class CreateBookingDto
    {
        public int ServiceId { get; set; }
        public DateTime BookingDate { get; set; }
        public int Hours { get; set; }
        public bool UseCredit { get; set; } = false;
    }

    public class VerifyBookingDto
    {
        public bool Verify { get; set; }
    }
}
