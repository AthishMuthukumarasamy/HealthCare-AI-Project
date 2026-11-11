
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Microsoft.Extensions.Logging;

namespace Healthcare.API.Services
{
    public class ProviderService : IProviderService
    {
        private readonly HealthcareDbContext _context;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(
            HealthcareDbContext context,
            ILogger<ProviderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<ProviderResponseDto> GetAllProviders()
        {
            _logger.LogInformation(
                "Retrieving all providers.");

            var providers = _context.Providers
                .Select(provider => new ProviderResponseDto
                {
                    Id = provider.Id,
                    FirstName = provider.FirstName,
                    LastName = provider.LastName,
                    Specialization = provider.Specialization,
                    Email = provider.Email,
                    PhoneNumber = provider.PhoneNumber,
                    YearsOfExperience = provider.YearsOfExperience
                })
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved {ProviderCount} providers.",
                providers.Count);

            return providers;
        }

        public ProviderResponseDto? GetProviderById(int id)
        {
            _logger.LogInformation(
                "Retrieving provider with ID {ProviderId}.", id);

            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
            {
                _logger.LogWarning(
                    "Provider with ID {ProviderId} was not found.", id);

                return null;
            }

            _logger.LogInformation(
                "Successfully retrieved provider with ID {ProviderId}.",
                id);

            return new ProviderResponseDto
            {
                Id = provider.Id,
                FirstName = provider.FirstName,
                LastName = provider.LastName,
                Specialization = provider.Specialization,
                Email = provider.Email,
                PhoneNumber = provider.PhoneNumber,
                YearsOfExperience = provider.YearsOfExperience
            };
        }

        public ProviderResponseDto CreateProvider(
            ProviderCreateDto providerDto)
        {
            _logger.LogInformation(
                "Creating a new provider.");

            var provider = new Provider
            {
                FirstName = providerDto.FirstName,
                LastName = providerDto.LastName,
                Specialization = providerDto.Specialization,
                Email = providerDto.Email,
                PhoneNumber = providerDto.PhoneNumber,
                YearsOfExperience = providerDto.YearsOfExperience
            };

            _context.Providers.Add(provider);
            _context.SaveChanges();

            _logger.LogInformation(
                "Provider created successfully with ID {ProviderId}.",
                provider.Id);

            return new ProviderResponseDto
            {
                Id = provider.Id,
                FirstName = provider.FirstName,
                LastName = provider.LastName,
                Specialization = provider.Specialization,
                Email = provider.Email,
                PhoneNumber = provider.PhoneNumber,
                YearsOfExperience = provider.YearsOfExperience
            };
        }

        public bool UpdateProvider(
            int id,
            ProviderCreateDto providerDto)
        {
            _logger.LogInformation(
                "Updating provider with ID {ProviderId}.", id);

            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
            {
                _logger.LogWarning(
                    "Cannot update provider. Provider with ID {ProviderId} was not found.",
                    id);

                return false;
            }

            provider.FirstName = providerDto.FirstName;
            provider.LastName = providerDto.LastName;
            provider.Specialization = providerDto.Specialization;
            provider.Email = providerDto.Email;
            provider.PhoneNumber = providerDto.PhoneNumber;
            provider.YearsOfExperience = providerDto.YearsOfExperience;

            _context.SaveChanges();

            _logger.LogInformation(
                "Provider with ID {ProviderId} updated successfully.",
                id);

            return true;
        }

        public bool DeleteProvider(int id)
        {
            _logger.LogInformation(
                "Deleting provider with ID {ProviderId}.", id);

            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
            {
                _logger.LogWarning(
                    "Cannot delete provider. Provider with ID {ProviderId} was not found.",
                    id);

                return false;
            }

            _context.Providers.Remove(provider);
            _context.SaveChanges();

            _logger.LogInformation(
                "Provider with ID {ProviderId} deleted successfully.",
                id);

            return true;
        }
    }
}
