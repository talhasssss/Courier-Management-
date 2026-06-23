using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models;

namespace ProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]  // All endpoints in this controller require Admin role
    public class AdminController : ControllerBase
    {
        private readonly CtmsDbContext _context;

        public AdminController(CtmsDbContext context)
        {
            _context = context;
        }

        // GET: api/Admin/couriers
        // Returns list of all couriers with ID, name, email, phone
        [HttpGet("couriers")]
        public async Task<ActionResult<IEnumerable<object>>> GetCouriers()
        {
            var couriers = await _context.Courier
                .Include(c => c.Contact)
                .Select(c => new
                {
                    courierId = c.CourierId,
                    name = c.Contact.Name,
                    email = c.Contact.Email,
                    phone = c.Contact.Phone.HasValue ? c.Contact.Phone.Value.ToString() : "Not provided"
                })
                .OrderBy(c => c.name)
                .ToListAsync();

            return Ok(couriers);
        }

        // GET: api/Admin/customers
        // Returns list of all customers with ID, name, email, phone, address
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomers()
        {
            var customers = await _context.Customer
                .Include(c => c.Contact)
                .Select(c => new
                {
                    customerId = c.CustomerId,
                    name = c.Contact.Name,
                    email = c.Contact.Email,
                    phone = c.Contact.Phone.HasValue ? c.Contact.Phone.Value.ToString() : "Not provided",
                    address = c.Contact.Address ?? "Not provided"
                })
                .OrderBy(c => c.name)
                .ToListAsync();

            return Ok(customers);
        }

        // PUT: api/Admin/assign/{trackingNumber}/{courierId}
        // Assign a courier to a package
        [HttpPut("assign/{trackingNumber}/{courierId}")]
        public async Task<IActionResult> AssignCourier(string trackingNumber, int courierId)
        {
            var package = await _context.Package
                .FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber.ToUpper());

            if (package == null)
                return NotFound("Package not found");

            var courier = await _context.Courier
                .Include(c => c.Contact)
                .FirstOrDefaultAsync(c => c.CourierId == courierId);

            if (courier == null)
                return NotFound("Courier not found");

            package.CourierId = courierId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Courier assigned successfully",
                trackingNumber = package.TrackingNumber,
                courierName = courier.Contact.Name
            });
        }
    }
}