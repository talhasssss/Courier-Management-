using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectAPI.Data;
using ProjectAPI.DTOs;
using ProjectAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CtmsDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(CtmsDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var contact = await _context.Contact
                .FirstOrDefaultAsync(c => c.Email == dto.Email);

            if (contact == null)
                return Unauthorized("Invalid email or password");

            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.ContactId == contact.ContactId);
            var courier = await _context.Courier.FirstOrDefaultAsync(c => c.ContactId == contact.ContactId);
            var admin = await _context.Admin.FirstOrDefaultAsync(a => a.ContactId == contact.ContactId);

            string? storedHash = customer?.PasswordHash ?? courier?.PasswordHash ?? admin?.PasswordHash;
            string role = customer != null ? "Customer" 
                        : courier != null ? "Courier" 
                        : admin != null ? "Admin" : "";

            if (string.IsNullOrEmpty(role) || storedHash != dto.Password)
                return Unauthorized("Invalid email or password");

            int roleId = customer?.CustomerId ?? courier?.CourierId ?? admin?.AdminId ?? 0;

            var token = GenerateJwtToken(contact.Email, role, contact.ContactId, roleId);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Role = role,
                UserId = contact.ContactId,
                RoleId = roleId
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {   

            if (string.IsNullOrWhiteSpace(dto.Name))
        return BadRequest("Full Name is required");

    if (string.IsNullOrWhiteSpace(dto.Email))
        return BadRequest("Email is required");

    if (string.IsNullOrWhiteSpace(dto.Password))
        return BadRequest("Password is required");

    if (string.IsNullOrWhiteSpace(dto.Phone))
        return BadRequest("Phone Number is required");

            var existingContact = await _context.Contact.FirstOrDefaultAsync(c => c.Email == dto.Email);
            if (existingContact != null)
                return BadRequest("Email already registered");

            if (!long.TryParse(dto.Phone, out long phoneValue))
    {
        return BadRequest("Invalid phone number format. Use digits only (e.g., 923001234567)");
    }

            var contact = new Contact
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = phoneValue,
                Address = dto.Address
            };

            _context.Contact.Add(contact);
            await _context.SaveChangesAsync();

            string passwordHash = dto.Password;
            int roleId = 0;

            switch (dto.Role.ToLower())
            {
                case "customer":
    var customer = new Customer { ContactId = contact.ContactId, PasswordHash = passwordHash };
    _context.Customer.Add(customer);
    await _context.SaveChangesAsync();
    roleId = customer.CustomerId;  // ← lowercase Id
    break;

case "courier":
    var courier = new Courier { ContactId = contact.ContactId, PasswordHash = passwordHash };
    _context.Courier.Add(courier);
    await _context.SaveChangesAsync();
    roleId = courier.CourierId;  // ← lowercase Id
    break;

case "admin":
    var admin = new Admin { ContactId = contact.ContactId, PasswordHash = passwordHash };
    _context.Admin.Add(admin);
    await _context.SaveChangesAsync();
    roleId = admin.AdminId;  // ← lowercase Id
    break;

                default:
                    return BadRequest("Invalid role");
            }

            var token = GenerateJwtToken(contact.Email, dto.Role, contact.ContactId, roleId);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Role = dto.Role,
                UserId = contact.ContactId,
                RoleId = roleId
            });
        }

        private string GenerateJwtToken(string email, string role, int contactId, int roleId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("ContactId", contactId.ToString()),
                new Claim("RoleId", roleId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, roleId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}