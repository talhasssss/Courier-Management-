using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models;
using ProjectAPI.DTOs;  // ← Correct namespace
using System.Security.Claims;  // ← ADD THIS LINE


namespace ProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires login for all actions
    public class PackageController : ControllerBase
    {
        private readonly CtmsDbContext _context;

        public PackageController(CtmsDbContext context)
        {
            _context = context;
        }

        // POST: api/Package - Create package (Customer only)
        [HttpPost]
[Authorize(Roles = "Customer")]
public async Task<ActionResult<Package>> CreatePackage([FromBody] CreatePackageDto dto)
{
    var roleIdStr = User.FindFirst("RoleId")?.Value;
    if (!int.TryParse(roleIdStr, out int customerId))
        return Unauthorized("Invalid token");

    var package = new Package
    {
        CustomerId = customerId,  // ← Now correct CustomerID
        CourierId = null,
        Weight = dto.Weight,
        Status = "PENDING",
        CreatedAt = DateTime.UtcNow,
        TrackingNumber = "TRK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
        RateperKG = 5.00,
        Cost = dto.Weight * 5.00,
        DeliveryAddress = dto.DeliveryAddress
    };

    _context.Package.Add(package);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetPackage), new { id = package.PackageID }, package);
}


// GET: api/Package/my - Customer's own packages
[HttpGet("my")]
[Authorize(Roles = "Customer")]
public async Task<ActionResult<IEnumerable<object>>> GetMyPackages()
{
    try
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized("Email not found in token");
        
        var customer = await _context.Customer
            .Include(c => c.Contact)
            .FirstOrDefaultAsync(c => c.Contact.Email == userEmail);
        
        if (customer == null)
            return Ok(new List<object>());
        
        // CAST decimal to double in the query
        var packages = await _context.Package
            .Include(p => p.Customer.Contact)
            .Include(p => p.Courier.Contact)
            .Where(p => p.CustomerId == customer.CustomerId)
            .Select(p => new
            {
                p.PackageID,
                p.TrackingNumber,
                Weight = Convert.ToDouble(p.Weight),  // Use Convert.ToDouble
                p.Status,
                p.CreatedAt,
                Cost = Convert.ToDouble(p.Cost),
                DeliveryAddress = p.DeliveryAddress ?? "Not provided",      // Use Convert.ToDouble
                CustomerName = p.Customer.Contact.Name,
                CourierName = p.Courier != null ? p.Courier.Contact.Name : "Not Assigned"
            })
            .ToListAsync();
        
        return Ok(packages);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        return StatusCode(500, "Internal server error");
    }
}
        // GET: api/Package/{id}
        [HttpGet("{id}")]
public async Task<ActionResult<object>> GetPackage(int id)
{
    var package = await _context.Package
        .Where(p => p.PackageID == id)
        .Select(p => new
        {
            p.PackageID,
            p.TrackingNumber,
            p.Weight,
            p.Status,
            p.CreatedAt,
            CustomerName = p.Customer != null && p.Customer.Contact != null
                           ? p.Customer.Contact.Name
                           : "Unknown",
            CourierName = p.Courier != null && p.Courier.Contact != null
                          ? p.Courier.Contact.Name
                          : "Not Assigned"
        })
        .FirstOrDefaultAsync();

    if (package == null)
        return NotFound(new { message = "Package not found" });

    return Ok(package);
}


        // GET: api/Package/assigned - Courier only
        [HttpGet("assigned")]
[Authorize(Roles = "Courier")]
public async Task<ActionResult<IEnumerable<object>>> GetAssignedPackages()
{
    var roleIdStr = User.FindFirst("RoleId")?.Value;
    if (!int.TryParse(roleIdStr, out int courierId))
        return Unauthorized("Invalid token");

    var packages = await _context.Package
        .Include(p => p.Customer.Contact)
        .Where(p => p.CourierId == courierId)
        .Select(p => new
        {
            p.PackageID,
            p.TrackingNumber,
            p.Weight,
            p.Status,
            p.CreatedAt,
            DeliveryAddress = p.DeliveryAddress ?? "Not provided",
            CustomerName = p.Customer.Contact.Name ?? "Unknown"
        })
        .ToListAsync();

    return Ok(packages);
     }

        // GET: api/Package/all - Admin only
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllPackages()
        {
            var packages = await _context.Package
                .Include(p => p.Customer.Contact)
                .Include(p => p.Courier)  // Include Courier
        .ThenInclude(c => c!.Contact)  // Safe access
                .Select(p => new
                {
                    p.PackageID,
                    p.TrackingNumber,
                    p.Weight,
                    p.Status,
                    p.CreatedAt,
                    DeliveryAddress = p.DeliveryAddress ?? "Not provided",
                    CustomerName = p.Customer.Contact.Name,
                    CourierName = p.Courier != null ? p.Courier.Contact.Name : "Not Assigned"
                })
                .ToListAsync();

            return Ok(packages);
        }
        // GET: api/Package/track/{trackingNumber} - PUBLIC (no login)
[HttpGet("track/{trackingNumber}")]
[AllowAnonymous]
public async Task<ActionResult<object>> TrackPackage(string trackingNumber)
{
    var package = await _context.Package
        .FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber.ToUpper());

    if (package == null)
        return NotFound("Package not found");

    return Ok(new
    {
        package.TrackingNumber,
        package.Status,
        DeliveryAddress = package.DeliveryAddress ?? "Not provided"
    });
    } 

    // PUT: api/Package/assign/{trackingNumber}/{courierId} - Admin only
[HttpPut("assign/{trackingNumber}/{courierId}")]
[Authorize(Roles = "Admin")]
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

    return Ok(new { message = "Courier assigned successfully", trackingNumber, courierName = courier.Contact.Name });
    } 
        
        // PUT: api/Package/update-status/{trackingNumber} - Courier only
        [HttpPut("update-status/{trackingNumber}")]
[Authorize(Roles = "Courier")]
public async Task<IActionResult> UpdatePackageStatus(string trackingNumber, [FromBody] UpdateStatusDto dto)
{
    var package = await _context.Package
        .FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber);

    if (package == null)
        return NotFound("Package not found");

    var roleIdStr = User.FindFirst("RoleId")?.Value;
    if (!int.TryParse(roleIdStr, out int courierId))
        return Unauthorized("Invalid token");

    if (package.CourierId != courierId)
        return BadRequest("This package is not assigned to you");

    package.Status = dto.NewStatus;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Status updated successfully!", newStatus = dto.NewStatus });
     }

        
    }
}