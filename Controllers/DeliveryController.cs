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
    public class DeliveryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeliveryController(AppDbContext context)
        {
            _context = context;
        }

        // ========== DELIVERY PERSON: REGISTER/UPDATE LOCATION ==========
        [HttpPost("update-location")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            deliveryPerson.IsAvailable = request.IsAvailable;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Location updated successfully",
                latitude = request.Latitude,
                longitude = request.Longitude,
                isAvailable = request.IsAvailable,
                updatedAt = DateTime.UtcNow
            });
        }

        // ========== DELIVERY PERSON: GET MY ASSIGNED ORDERS ==========
        [HttpGet("my-orders")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetMyAssignedOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var orders = await _context.Orders
                .Where(o => o.DeliveryPersonId == deliveryPerson.Id)
                .Include(o => o.Customer)
                .Include(o => o.Seller)
                .ThenInclude(s => s != null ? s.User : null)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt,
                    o.DeliveredAt,
                    Customer = new
                    {
                        Name = o.Customer != null ? o.Customer.FullName : "Unknown",
                        Phone = o.Customer != null ? o.Customer.Phone : "Unknown",
                        Email = o.Customer != null ? o.Customer.Email : "Unknown"
                    },
                    Seller = new
                    {
                        BusinessName = o.Seller != null ? o.Seller.BusinessName : "Unknown",
                        Phone = o.Seller != null && o.Seller.User != null ? o.Seller.User.Phone : "Unknown"
                    },
                    DeliveryAddress = "Customer Address"
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var pendingPickup = orders.Where(o => o.Status == "AssignedToDelivery" || o.Status == "SellerConfirmed").ToList();
            var pickedUp = orders.Where(o => o.Status == "PickedUp").ToList();
            var outForDelivery = orders.Where(o => o.Status == "OutForDelivery").ToList();
            var arrived = orders.Where(o => o.Status == "Arrived").ToList();
            var delivered = orders.Where(o => o.Status == "Delivered" || o.Status == "Completed").ToList();

            return Ok(new
            {
                Summary = new
                {
                    TotalAssigned = orders.Count,
                    PendingPickup = pendingPickup.Count,
                    PickedUp = pickedUp.Count,
                    OutForDelivery = outForDelivery.Count,
                    Arrived = arrived.Count,
                    Delivered = delivered.Count
                },
                Orders = new
                {
                    PendingPickup = pendingPickup,
                    PickedUp = pickedUp,
                    OutForDelivery = outForDelivery,
                    Arrived = arrived,
                    Delivered = delivered
                }
            });
        }

        // ========== DELIVERY PERSON: MARK ORDER AS PICKED UP ==========
        [HttpPut("pickup/{orderId}")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> MarkAsPickedUp(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var order = await _context.Orders
                .Include(o => o.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeliveryPersonId == deliveryPerson.Id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found or not assigned to you" });
            }

            if (order.Status != "AssignedToDelivery" && order.Status != "SellerConfirmed")
            {
                return BadRequest(new { message = $"Cannot pick up order with status: {order.Status}" });
            }

            order.Status = "PickedUp";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order picked up successfully",
                orderId = order.Id,
                order.Status,
                pickedUpAt = DateTime.UtcNow
            });
        }

        // ========== DELIVERY PERSON: MARK AS OUT FOR DELIVERY ==========
        [HttpPut("out-for-delivery/{orderId}")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> MarkAsOutForDelivery(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeliveryPersonId == deliveryPerson.Id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found or not assigned to you" });
            }

            if (order.Status != "PickedUp")
            {
                return BadRequest(new { message = $"Order must be picked up first. Current status: {order.Status}" });
            }

            order.Status = "OutForDelivery";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order is out for delivery",
                orderId = order.Id,
                order.Status,
                outForDeliveryAt = DateTime.UtcNow
            });
        }

        // ========== DELIVERY PERSON: MARK AS ARRIVED AT LOCATION ==========
        [HttpPut("arrived/{orderId}")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> MarkAsArrived(int orderId, [FromBody] ArrivedDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeliveryPersonId == deliveryPerson.Id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found or not assigned to you" });
            }

            if (order.Status != "OutForDelivery")
            {
                return BadRequest(new { message = $"Order must be out for delivery. Current status: {order.Status}" });
            }

            order.Status = "Arrived";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Arrived at customer location",
                orderId = order.Id,
                order.Status,
                arrivedAt = DateTime.UtcNow,
                latitude = request.Latitude,
                longitude = request.Longitude
            });
        }

        // ========== DELIVERY PERSON: MARK AS DELIVERED ==========
        [HttpPut("delivered/{orderId}")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> MarkAsDelivered(int orderId, [FromBody] DeliveredDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeliveryPersonId == deliveryPerson.Id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found or not assigned to you" });
            }

            if (order.Status != "Arrived")
            {
                return BadRequest(new { message = $"Order must be marked as arrived first. Current status: {order.Status}" });
            }

            order.Status = "Delivered";
            order.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var journalEntry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                AccountDebit = "Delivery Expense",
                AccountCredit = "Cash/Bank",
                Amount = 50,
                Narration = $"Delivery completed for order #{order.Id}",
                OrderId = order.Id
            };
            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order delivered successfully!",
                orderId = order.Id,
                order.Status,
                deliveredAt = order.DeliveredAt,
                otpVerified = request.OtpCode,
                signature = request.Signature
            });
        }

        // ========== DELIVERY PERSON: GET DELIVERY HISTORY ==========
        [HttpGet("delivery-history")]
        [Authorize(Roles = "Delivery")]
        public async Task<IActionResult> GetDeliveryHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var userId = int.Parse(userIdClaim.Value);
            var deliveryPerson = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (deliveryPerson == null)
            {
                return BadRequest(new { message = "Delivery profile not found" });
            }

            var startDate = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = toDate ?? DateTime.UtcNow;

            var deliveries = await _context.Orders
                .Where(o => o.DeliveryPersonId == deliveryPerson.Id &&
                       o.Status == "Delivered" &&
                       o.DeliveredAt >= startDate &&
                       o.DeliveredAt <= endDate)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.DeliveredAt,
                    CustomerName = o.Customer != null ? o.Customer.FullName : "Unknown",
                    DeliveryDuration = o.DeliveredAt != null ? (o.DeliveredAt.Value - o.CreatedAt).TotalHours : 0
                })
                .OrderByDescending(o => o.DeliveredAt)
                .ToListAsync();

            var totalDeliveries = deliveries.Count;
            var totalEarnings = totalDeliveries * 50;
            var averageDeliveryTime = deliveries.Any() ? deliveries.Average(d => d.DeliveryDuration) : 0;

            return Ok(new
            {
                Period = new { From = startDate, To = endDate },
                Summary = new
                {
                    TotalDeliveries = totalDeliveries,
                    TotalEarnings = totalEarnings,
                    AverageDeliveryTimeHours = Math.Round(averageDeliveryTime, 2)
                },
                Deliveries = deliveries
            });
        }

        // ========== ADMIN: GET ALL DELIVERY PERSONS ==========
        [HttpGet("admin/all-delivery-persons")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDeliveryPersons()
        {
            var deliveryPersons = await _context.DeliveryPersons
                .Include(d => d.User)
                .Select(d => new
                {
                    d.Id,
                    d.UserId,
                    Name = d.User != null ? d.User.FullName : "Unknown",
                    Email = d.User != null ? d.User.Email : "Unknown",
                    Phone = d.User != null ? d.User.Phone : "Unknown",
                    d.VehicleType,
                    d.Zone,
                    d.IsAvailable,
                    TotalDeliveries = _context.Orders.Count(o => o.DeliveryPersonId == d.Id),
                    CompletedDeliveries = _context.Orders.Count(o => o.DeliveryPersonId == d.Id && o.Status == "Delivered"),
                    CurrentOrders = _context.Orders.Count(o => o.DeliveryPersonId == d.Id && o.Status != "Delivered" && o.Status != "Completed")
                })
                .ToListAsync();

            return Ok(deliveryPersons);
        }

        // ========== ADMIN: GET AVAILABLE DELIVERY PERSONS ==========
        [HttpGet("admin/available")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAvailableDeliveryPersons()
        {
            var availableDeliveries = await _context.DeliveryPersons
                .Where(d => d.IsAvailable == true)
                .Include(d => d.User)
                .Select(d => new
                {
                    d.Id,
                    Name = d.User != null ? d.User.FullName : "Unknown",
                    Phone = d.User != null ? d.User.Phone : "Unknown",
                    d.VehicleType,
                    d.Zone,
                    CurrentLoad = _context.Orders.Count(o => o.DeliveryPersonId == d.Id && o.Status != "Delivered" && o.Status != "Completed")
                })
                .ToListAsync();

            return Ok(availableDeliveries);
        }

        // ========== ADMIN: ASSIGN DELIVERY PERSON TO ORDER ==========
        [HttpPost("admin/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignDeliveryToOrder([FromBody] AssignDeliveryOrderDto request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            var deliveryPerson = await _context.DeliveryPersons
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DeliveryPersonId);

            if (deliveryPerson == null)
            {
                return NotFound(new { message = "Delivery person not found" });
            }

            order.DeliveryPersonId = request.DeliveryPersonId;
            order.Status = "AssignedToDelivery";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Order #{request.OrderId} assigned to {deliveryPerson.User?.FullName}",
                orderId = order.Id,
                deliveryPersonName = deliveryPerson.User?.FullName,
                order.Status
            });
        }

        // ========== ADMIN: ADD NEW DELIVERY PERSON ==========
        [HttpPost("admin/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDeliveryPerson([FromBody] AddDeliveryPersonDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                user = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Role = "Delivery",
                    IsVerified = true,
                    VerifiedByAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                    CreatedAt = DateTime.UtcNow,
                    CreditBalance = 0
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var existingDelivery = await _context.DeliveryPersons
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (existingDelivery != null)
            {
                return BadRequest(new { message = "Delivery person already exists for this user" });
            }

            var deliveryPerson = new DeliveryPerson
            {
                UserId = user.Id,
                VehicleType = request.VehicleType,
                Zone = request.Zone,
                IsAvailable = true
            };

            _context.DeliveryPersons.Add(deliveryPerson);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Delivery person added successfully",
                deliveryPersonId = deliveryPerson.Id,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Phone
                }
            });
        }

        // ========== ADMIN: UPDATE DELIVERY PERSON STATUS ==========
        [HttpPut("admin/update-status/{deliveryPersonId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDeliveryPersonStatus(int deliveryPersonId, [FromBody] UpdateDeliveryStatusDto request)
        {
            var deliveryPerson = await _context.DeliveryPersons
                .FindAsync(deliveryPersonId);

            if (deliveryPerson == null)
            {
                return NotFound(new { message = "Delivery person not found" });
            }

            deliveryPerson.IsAvailable = request.IsAvailable;
            deliveryPerson.Zone = request.Zone ?? deliveryPerson.Zone;
            deliveryPerson.VehicleType = request.VehicleType ?? deliveryPerson.VehicleType;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Delivery person status updated. Available: {request.IsAvailable}",
                deliveryPersonId = deliveryPerson.Id,
                deliveryPerson.IsAvailable,
                deliveryPerson.Zone,
                deliveryPerson.VehicleType
            });
        }

        // ========== ADMIN: DELETE DELIVERY PERSON ==========
        [HttpDelete("admin/delete/{deliveryPersonId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDeliveryPerson(int deliveryPersonId)
        {
            var deliveryPerson = await _context.DeliveryPersons
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == deliveryPersonId);

            if (deliveryPerson == null)
            {
                return NotFound(new { message = "Delivery person not found" });
            }

            var userId = deliveryPerson.UserId;

            var pendingOrders = await _context.Orders
                .Where(o => o.DeliveryPersonId == deliveryPersonId && o.Status != "Delivered" && o.Status != "Completed")
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                order.DeliveryPersonId = null;
                order.Status = "AdminVerified";
            }

            _context.DeliveryPersons.Remove(deliveryPerson);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Delivery person deleted successfully",
                unassignedOrders = pendingOrders.Count
            });
        }

        // ========== ADMIN: GET DELIVERY STATISTICS ==========
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeliveryStatistics()
        {
            var totalDeliveryPersons = await _context.DeliveryPersons.CountAsync();
            var availableDeliveryPersons = await _context.DeliveryPersons.CountAsync(d => d.IsAvailable == true);
            var busyDeliveryPersons = totalDeliveryPersons - availableDeliveryPersons;

            var totalDeliveries = await _context.Orders.CountAsync(o => o.Status == "Delivered" || o.Status == "Completed");
            var pendingDeliveries = await _context.Orders.CountAsync(o => o.DeliveryPersonId != null && o.Status != "Delivered" && o.Status != "Completed");
            var unassignedDeliveries = await _context.Orders.CountAsync(o => o.DeliveryPersonId == null && o.Status == "AdminVerified");

            var today = DateTime.UtcNow.Date;
            var todayDeliveries = await _context.Orders
                .CountAsync(o => o.DeliveredAt != null && o.DeliveredAt.Value.Date == today);

            var deliveredOrders = await _context.Orders
                .Where(o => o.DeliveredAt != null)
                .Select(o => new { o.CreatedAt, o.DeliveredAt })
                .ToListAsync();

            var averageDeliveryTime = deliveredOrders.Any()
                ? deliveredOrders.Average(o => (o.DeliveredAt.Value - o.CreatedAt).TotalHours)
                : 0;

            return Ok(new
            {
                DeliveryPersonnel = new
                {
                    Total = totalDeliveryPersons,
                    Available = availableDeliveryPersons,
                    Busy = busyDeliveryPersons
                },
                Deliveries = new
                {
                    TotalCompleted = totalDeliveries,
                    Pending = pendingDeliveries,
                    Unassigned = unassignedDeliveries,
                    Today = todayDeliveries
                },
                Performance = new
                {
                    AverageDeliveryTimeHours = Math.Round(averageDeliveryTime, 2)
                }
            });
        }
    }

    // ========== DTO CLASSES ==========

    public class UpdateLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class ArrivedDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DeliveredDto
    {
        public string OtpCode { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class AssignDeliveryOrderDto
    {
        public int OrderId { get; set; }
        public int DeliveryPersonId { get; set; }
    }

    public class AddDeliveryPersonDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
    }

    public class UpdateDeliveryStatusDto
    {
        public bool IsAvailable { get; set; }
        public string? Zone { get; set; }
        public string? VehicleType { get; set; }
    }
}
