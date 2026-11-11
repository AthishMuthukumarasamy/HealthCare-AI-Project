
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

        // Admin only: View, search, filter, sort and paginate providers
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllProviders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? specialization = null,
            [FromQuery] string sortBy = "id",
            [FromQuery] string sortOrder = "asc")
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page number must be at least 1 and page size must be between 1 and 100."
                });
            }

            var allowedSortFields = new[]
            {
                "id",
                "firstname",
                "lastname",
                "specialization",
                "email",
                "yearsofexperience"
            };

            sortBy = sortBy.Trim().ToLowerInvariant();
            sortOrder = sortOrder.Trim().ToLowerInvariant();

            if (!allowedSortFields.Contains(sortBy))
            {
                return BadRequest(new
                {
                    message = "Invalid sortBy value. Allowed values: id, firstName, lastName, specialization, email, yearsOfExperience."
                });
            }

            if (sortOrder != "asc" && sortOrder != "desc")
            {
                return BadRequest(new
                {
                    message = "Invalid sortOrder value. Allowed values: asc, desc."
                });
            }

            var query = _context.Providers
                .AsNoTracking()
                .AsQueryable();

            // Search providers by first name or last name.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(p =>
                    p.FirstName.Contains(searchTerm) ||
                    p.LastName.Contains(searchTerm));
            }

            // Filter providers by specialization.
            if (!string.IsNullOrWhiteSpace(specialization))
            {
                var specializationTerm = specialization.Trim();

                query = query.Where(p =>
                    p.Specialization.Contains(specializationTerm));
            }

            // Apply sorting.
            query = (sortBy, sortOrder) switch
            {
                ("firstname", "asc") =>
                    query.OrderBy(p => p.FirstName).ThenBy(p => p.Id),

                ("firstname", "desc") =>
                    query.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id),

                ("lastname", "asc") =>
                    query.OrderBy(p => p.LastName).ThenBy(p => p.Id),

                ("lastname", "desc") =>
                    query.OrderByDescending(p => p.LastName).ThenBy(p => p.Id),

                ("specialization", "asc") =>
                    query.OrderBy(p => p.Specialization).ThenBy(p => p.Id),

                ("specialization", "desc") =>
                    query.OrderByDescending(p => p.Specialization).ThenBy(p => p.Id),

                ("email", "asc") =>
                    query.OrderBy(p => p.Email).ThenBy(p => p.Id),

                ("email", "desc") =>
                    query.OrderByDescending(p => p.Email).ThenBy(p => p.Id),

                ("yearsofexperience", "asc") =>
                    query.OrderBy(p => p.YearsOfExperience).ThenBy(p => p.Id),

                ("yearsofexperience", "desc") =>
                    query.OrderByDescending(p => p.YearsOfExperience).ThenBy(p => p.Id),

                ("id", "desc") =>
                    query.OrderByDescending(p => p.Id),

                _ => query.OrderBy(p => p.Id)
            };

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
