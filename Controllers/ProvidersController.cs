
using Healthcare.API.DTOs;
using Healthcare.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProvidersController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        [HttpGet]
        public IActionResult GetAllProviders()
        {
            return Ok(_providerService.GetAllProviders());
        }

        [HttpGet("{id}")]
        public IActionResult GetProviderById(int id)
        {
            var provider = _providerService.GetProviderById(id);

            if (provider == null)
                return NotFound();

            return Ok(provider);
        }

        [HttpPost]
        public IActionResult CreateProvider(ProviderCreateDto providerDto)
        {
            var provider = _providerService.CreateProvider(providerDto);

            return CreatedAtAction(
                nameof(GetProviderById),
                new { id = provider.Id },
                provider);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateProvider(int id, ProviderCreateDto providerDto)
        {
            var updated = _providerService.UpdateProvider(id, providerDto);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProvider(int id)
        {
            var deleted = _providerService.DeleteProvider(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
