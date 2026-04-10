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
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WalletController(AppDbContext context)
        {
            _context = context;
        }

        // ========== CUSTOMER: GET MY CREDIT BALANCE ==========
        [HttpGet("my-balance")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyCreditBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                userId = user.Id,
                user.FullName,
                CreditBalance = user.CreditBalance,
                Currency = "NPR"
            });
        }

        // ========== CUSTOMER: GET CREDIT TRANSACTION HISTORY ==========
        [HttpGet("my-transactions")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyTransactions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);

            // Get credit additions from journal entries (when admin adds credit)
            var creditAdditions = await _context.JournalEntries
                .Where(j => j.Narration.Contains($"Added credit to user {userId}") ||
                       j.Narration.Contains($"Credit added for customer {userId}"))
                .Select(j => new
                {
                    Type = "Credit Added",
                    j.Date,
                    j.Amount,
                    j.Narration,
                    Balance = 0 // Will calculate
                })
                .ToListAsync();

            // Get credit usage from orders
            var ordersWithCredit = await _context.Orders
                .Where(o => o.CustomerId == userId)
                .Select(o => new
                {
                    Type = "Credit Used",
                    Date = o.CreatedAt,
                    Amount = 0, // We don't store credit used separately in Order table
                    Narration = $"Order #{o.Id}",
                    OrderTotal = o.TotalAmount
                })
                .ToListAsync();

            var allTransactions = creditAdditions.Cast<object>()
                .Concat(ordersWithCredit)
                .OrderByDescending(t => ((dynamic)t).Date)
                .ToList();

            return Ok(new
            {
                CurrentBalance = (await _context.Users.FindAsync(userId))?.CreditBalance ?? 0,
                Transactions = allTransactions
            });
        }

        // ========== SELLER: GET EARNINGS SUMMARY ==========
        [HttpGet("seller/earnings")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerEarnings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
            {
                return BadRequest(new { message = "Seller profile not found" });
            }

            var completedOrders = await _context.Orders
                .Where(o => o.SellerId == seller.Id && o.Status == "Completed")
                .ToListAsync();

            var totalEarnings = completedOrders.Sum(o => o.SellerEarnings);
            var totalCommission = completedOrders.Sum(o => o.CommissionAmount);
            var totalOrders = completedOrders.Count;
            var pendingOrders = await _context.Orders
                .CountAsync(o => o.SellerId == seller.Id && o.Status != "Completed");

            // Monthly breakdown
            var monthlyEarnings = await _context.Orders
                .Where(o => o.SellerId == seller.Id && o.Status == "Completed")
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Earnings = g.Sum(o => o.SellerEarnings),
                    Orders = g.Count()
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToListAsync();

            return Ok(new
            {
                Summary = new
                {
                    TotalEarnings = totalEarnings,
                    TotalCommission = totalCommission,
                    TotalOrders = totalOrders,
                    PendingOrders = pendingOrders,
                    AverageOrderValue = totalOrders > 0 ? totalEarnings / totalOrders : 0
                },
                MonthlyBreakdown = monthlyEarnings
            });
        }

        // ========== SERVICE PROVIDER: GET EARNINGS SUMMARY ==========
        [HttpGet("provider/earnings")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> GetProviderEarnings()
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

            var completedBookings = await _context.ServiceBookings
                .Where(b => b.ProviderId == provider.Id && b.Status == "Completed")
                .Include(b => b.Service)
                .ToListAsync();

            var totalEarnings = completedBookings.Sum(b => b.TotalAmount - b.VatAmount);
            var totalVat = completedBookings.Sum(b => b.VatAmount);
            var totalBookings = completedBookings.Count;
            var pendingBookings = await _context.ServiceBookings
                .CountAsync(b => b.ProviderId == provider.Id && b.Status != "Completed");

            // Monthly breakdown
            var monthlyEarnings = await _context.ServiceBookings
                .Where(b => b.ProviderId == provider.Id && b.Status == "Completed")
                .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Earnings = g.Sum(b => b.TotalAmount - b.VatAmount),
                    Bookings = g.Count()
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToListAsync();

            return Ok(new
            {
                Summary = new
                {
                    TotalEarnings = totalEarnings,
                    TotalVAT = totalVat,
                    TotalBookings = totalBookings,
                    PendingBookings = pendingBookings,
                    AverageBookingValue = totalBookings > 0 ? totalEarnings / totalBookings : 0
                },
                MonthlyBreakdown = monthlyEarnings
            });
        }

        // ========== SELLER: REQUEST WITHDRAWAL ==========
        [HttpPost("seller/withdraw")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> RequestSellerWithdrawal([FromBody] WithdrawalDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
            {
                return BadRequest(new { message = "Seller profile not found" });
            }

            // Calculate available balance (completed orders only)
            var completedOrders = await _context.Orders
                .Where(o => o.SellerId == seller.Id && o.Status == "Completed")
                .ToListAsync();

            var availableBalance = completedOrders.Sum(o => o.SellerEarnings);

            if (request.Amount > availableBalance)
            {
                return BadRequest(new { message = $"Insufficient balance. Available: {availableBalance}" });
            }

            if (request.Amount < 500)
            {
                return BadRequest(new { message = "Minimum withdrawal amount is NPR 500" });
            }

            // Create withdrawal request (you would store this in a WithdrawalRequests table)
            // For now, we'll create a journal entry
            var journalEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Seller Payable",
                AccountCredit = "Bank",
                Amount = request.Amount,
                Narration = $"Withdrawal request for seller {seller.BusinessName} - {request.BankAccount}",
                OrderId = null
            };
            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Withdrawal request submitted successfully. Admin will process within 3-5 business days.",
                requestId = journalEntry.Id,
                request.Amount,
                request.BankAccount,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            });
        }

        // ========== SERVICE PROVIDER: REQUEST WITHDRAWAL ==========
        [HttpPost("provider/withdraw")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> RequestProviderWithdrawal([FromBody] WithdrawalDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var provider = await _context.ServicePros
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return BadRequest(new { message = "Service provider profile not found" });
            }

            // Calculate available balance (completed bookings only)
            var completedBookings = await _context.ServiceBookings
                .Where(b => b.ProviderId == provider.Id && b.Status == "Completed")
                .ToListAsync();

            var availableBalance = completedBookings.Sum(b => b.TotalAmount - b.VatAmount);

            if (request.Amount > availableBalance)
            {
                return BadRequest(new { message = $"Insufficient balance. Available: {availableBalance}" });
            }

            if (request.Amount < 500)
            {
                return BadRequest(new { message = "Minimum withdrawal amount is NPR 500" });
            }

            // Create withdrawal request
            var journalEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Provider Payable",
                AccountCredit = "Bank",
                Amount = request.Amount,
                Narration = $"Withdrawal request for provider {provider.LicenseNumber} - {request.BankAccount}",
                ServiceBookingId = null
            };
            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Withdrawal request submitted successfully. Admin will process within 3-5 business days.",
                requestId = journalEntry.Id,
                request.Amount,
                request.BankAccount,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            });
        }

        // ========== ADMIN: ADD CREDIT TO CUSTOMER ==========
        [HttpPost("admin/add-credit")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAddCredit([FromBody] AdminAddCreditDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var adminId = adminIdClaim != null ? int.Parse(adminIdClaim.Value) : 0;

            user.CreditBalance += request.Amount;

            // Create journal entry
            var journalEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Credit Given",
                AccountCredit = "Customer Credit Liability",
                Amount = request.Amount,
                Narration = $"Admin added credit of {request.Amount} to user {user.Email}. Reason: {request.Reason}",
                OrderId = null
            };
            _context.JournalEntries.Add(journalEntry);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Added {request.Amount} credit to {user.FullName}",
                userId = user.Id,
                user.FullName,
                NewBalance = user.CreditBalance,
                Reason = request.Reason,
                AddedByAdminId = adminId,
                AddedAt = DateTime.UtcNow
            });
        }

        // ========== ADMIN: GET ALL WITHDRAWAL REQUESTS ==========
        [HttpGet("admin/withdrawal-requests")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetWithdrawalRequests()
        {
            // Get withdrawal requests from journal entries
            var withdrawalRequests = await _context.JournalEntries
                .Where(j => j.AccountDebit == "Seller Payable" || j.AccountDebit == "Provider Payable")
                .Select(j => new
                {
                    j.Id,
                    j.Date,
                    j.Amount,
                    j.Narration,
                    Type = j.AccountDebit == "Seller Payable" ? "Seller" : "ServiceProvider"
                })
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            var totalPendingAmount = withdrawalRequests.Sum(w => w.Amount);

            return Ok(new
            {
                TotalPendingRequests = withdrawalRequests.Count,
                TotalPendingAmount = totalPendingAmount,
                Requests = withdrawalRequests
            });
        }

        // ========== ADMIN: PROCESS WITHDRAWAL ==========
        [HttpPost("admin/process-withdrawal/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessWithdrawal(int requestId, [FromBody] ProcessWithdrawalDto request)
        {
            var journalEntry = await _context.JournalEntries.FindAsync(requestId);

            if (journalEntry == null)
            {
                return NotFound(new { message = "Withdrawal request not found" });
            }

            // Update the journal entry to mark as processed
            journalEntry.Narration = $"{journalEntry.Narration} - PROCESSED: {request.TransactionId}";

            // Create a new journal entry for the payout
            var payoutEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Bank",
                AccountCredit = journalEntry.AccountDebit == "Seller Payable" ? "Seller Payable" : "Provider Payable",
                Amount = journalEntry.Amount,
                Narration = $"Withdrawal processed - Transaction ID: {request.TransactionId}",
                OrderId = journalEntry.OrderId,
                ServiceBookingId = journalEntry.ServiceBookingId
            };
            _context.JournalEntries.Add(payoutEntry);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Withdrawal processed successfully",
                requestId = requestId,
                transactionId = request.TransactionId,
                processedAt = DateTime.UtcNow
            });
        }

        // ========== ADMIN: GET PLATFORM FINANCIAL SUMMARY ==========
        [HttpGet("admin/financial-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFinancialSummary()
        {
            // Total platform revenue from commissions
            var totalProductCommission = await _context.Orders.SumAsync(o => o.CommissionAmount);
            var totalServiceCommission = await _context.ServiceBookings.SumAsync(b => b.TotalAmount * 0.15m); // Approximate

            // Total VAT collected
            var totalVat = await _context.Orders.SumAsync(o => o.VatAmount) +
                           await _context.ServiceBookings.SumAsync(b => b.VatAmount);

            // Total payouts made to sellers
            var totalPayoutsToSellers = await _context.JournalEntries
                .Where(j => j.AccountDebit == "Bank" && j.AccountCredit == "Seller Payable")
                .SumAsync(j => j.Amount);

            // Total payouts to providers
            var totalPayoutsToProviders = await _context.JournalEntries
                .Where(j => j.AccountDebit == "Bank" && j.AccountCredit == "Provider Payable")
                .SumAsync(j => j.Amount);

            // Current outstanding payables
            var outstandingSellerPayable = await _context.JournalEntries
                .Where(j => j.AccountDebit == "Seller Payable")
                .SumAsync(j => j.Amount) - totalPayoutsToSellers;

            var outstandingProviderPayable = await _context.JournalEntries
                .Where(j => j.AccountDebit == "Provider Payable")
                .SumAsync(j => j.Amount) - totalPayoutsToProviders;

            return Ok(new
            {
                Revenue = new
                {
                    ProductCommission = totalProductCommission,
                    ServiceCommission = totalServiceCommission,
                    TotalCommission = totalProductCommission + totalServiceCommission,
                    TotalVATCollected = totalVat
                },
                Payouts = new
                {
                    PaidToSellers = totalPayoutsToSellers,
                    PaidToProviders = totalPayoutsToProviders,
                    TotalPaid = totalPayoutsToSellers + totalPayoutsToProviders
                },
                Outstanding = new
                {
                    SellerPayable = outstandingSellerPayable,
                    ProviderPayable = outstandingProviderPayable,
                    TotalPayable = outstandingSellerPayable + outstandingProviderPayable
                },
                PlatformNetRevenue = (totalProductCommission + totalServiceCommission) - (totalPayoutsToSellers + totalPayoutsToProviders)
            });
        }

        // ========== ADMIN: GET CREDIT BALANCE FOR ANY USER ==========
        [HttpGet("admin/user-credit/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserCreditBalance(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get credit history
            var creditHistory = await _context.JournalEntries
                .Where(j => j.Narration.Contains($"added credit to user {userId}"))
                .Select(j => new
                {
                    j.Date,
                    j.Amount,
                    j.Narration
                })
                .ToListAsync();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                CurrentCreditBalance = user.CreditBalance,
                TotalCreditAdded = creditHistory.Sum(c => c.Amount),
                CreditHistory = creditHistory
            });
        }
    }

    // ========== DTO CLASSES ==========

    public class WithdrawalDto
    {
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
    }

    public class AdminAddCreditDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ProcessWithdrawalDto
    {
        public string TransactionId { get; set; } = string.Empty;
    }
}
