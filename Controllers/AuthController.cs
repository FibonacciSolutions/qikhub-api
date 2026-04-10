using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using QikHubAPI.Data;
using QikHubAPI.Models;
using BCrypt.Net;

namespace QikHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            if (user.PasswordHash != request.CurrentPassword)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.PasswordHash = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        public class ChangePasswordDto
        {
            public int UserId { get; set; }
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if password is BCrypt hash or plain text
            bool isPasswordValid = false;

            if (user.PasswordHash.StartsWith("$2a$"))
            {
                // BCrypt hash
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            else
            {
                // Plain text
                isPasswordValid = (user.PasswordHash == request.Password);
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Phone,
                    user.Role,
                    user.IsVerified,
                    user.CreditBalance
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Hash password with BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                Phone = request.Phone,
                Role = request.Role,
                IsVerified = request.Role == "Customer",
                CreatedAt = DateTime.UtcNow,
                CreditBalance = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful",
                userId = user.Id,
                role = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes("QikHubSuperSecretKey2024ForJWTTokenGeneration!@#$%");
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("FullName", user.FullName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? TaxId { get; set; }
        public string? BankAccount { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiry { get; set; }
        public string? InsuranceDoc { get; set; }
        public string? VehicleType { get; set; }
        public string? Zone { get; set; }
    }
}