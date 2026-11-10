
using System.Security.Claims;
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderService _providerService;
        private readonly HealthcareDbContext _context;

        public ProvidersController(
            IProviderService providerService,
            HealthcareDbContext context)
        {
            _providerService = providerService;
            _context = context;
        }

        // Admin only: View all providers with pagination
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllProviders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page number must be at least 1 and page size must be between 1 and 100."
                });
            }

            var query = _context.Providers
                .AsNoTracking()
                .OrderBy(p => p.Id);

            var totalRecords = await query.CountAsync();

            var providers = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProviderResponseDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Specialization = p.Specialization,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    YearsOfExperience = p.YearsOfExperience
                })
                .ToListAsync();

            var response = new PagedResponse<ProviderResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(
                    (double)totalRecords / pageSize),
                Data = providers
            };

            return Ok(response);
        }

        // Admin or Doctor: View provider by ID
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetProviderById(int id)
        {
            var provider = _providerService.GetProviderById(id);

            if (provider == null)
                return NotFound();

            if (User.IsInRole("Admin"))
                return Ok(provider);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var hasAccess = await _context.Providers.AnyAsync(
                p => p.Id == id && p.ApplicationUserId == userId);

            if (!hasAccess)
                return NotFound();

            return Ok(provider);
        }

        // Admin or Doctor: View own provider profile
        [HttpGet("my-profile")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (User.IsInRole("Admin"))
                return BadRequest(new
                {
                    message = "Admins should use GET /api/providers to view providers."
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var provider = await _context.Providers
                .Where(p => p.ApplicationUserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (provider == 0)
                return NotFound();

            return Ok(_providerService.GetProviderById(provider));
        }

        // Admin only: Create provider
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProvider(ProviderCreateDto providerDto)
        {
            var provider = _providerService.CreateProvider(providerDto);

            return CreatedAtAction(
                nameof(GetProviderById),
                new { id = provider.Id },
                provider);
        }

        // Admin or owning Doctor: Update provider
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> UpdateProvider(
            int id,
            ProviderCreateDto providerDto)
        {
            if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var hasAccess = await _context.Providers.AnyAsync(
                    p => p.Id == id && p.ApplicationUserId == userId);

                if (!hasAccess)
                    return NotFound();
            }

            var updated = _providerService.UpdateProvider(id, providerDto);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        // Admin only: Delete provider
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProvider(int id)
        {
            var deleted = _providerService.DeleteProvider(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
