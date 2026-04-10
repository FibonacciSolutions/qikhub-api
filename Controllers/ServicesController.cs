using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QikHubAPI.Data;
using QikHubAPI.Models;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllApprovedServices()
        {
            var services = await _context.Services
                .Where(s => s.AdminApproved == true)
                .Select(s => new
                {
                    s.Id,
                    Title = s.Name,
                    s.Description,
                    s.PricePerHour,
                    s.DurationMinutes
                })
                .ToListAsync();
            return Ok(services);
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAllServices()
        {
            var services = await _context.Services
                .Select(s => new
                {
                    s.Id,
                    Title = s.Name,
                    s.Description,
                    s.PricePerHour,
                    s.DurationMinutes,
                    s.AdminApproved
                })
                .ToListAsync();
            return Ok(services);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceDto request)
        {
            var provider = await _context.ServicePros.FirstOrDefaultAsync();
            if (provider == null)
            {
                provider = new ServicePro
                {
                    UserId = 1,
                    LicenseNumber = "ADMIN001",
                    LicenseExpiry = DateTime.UtcNow.AddYears(5),
                    InsuranceDoc = "admin.pdf",
                    CommissionRate = 15,
                    Status = "Approved"
                };
                _context.ServicePros.Add(provider);
                await _context.SaveChangesAsync();
            }

            var service = new Service
            {
                ProviderId = provider.Id,
                Name = request.Title,
                Description = request.Description ?? "",
                PricePerHour = request.PricePerHour,
                DurationMinutes = request.DurationMinutes,
                AdminApproved = true
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Service created successfully", serviceId = service.Id });
        }

        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound(new { message = "Service not found" });
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Service deleted successfully" });
        }
    }

    public class CreateServiceDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public int DurationMinutes { get; set; }
    }
}