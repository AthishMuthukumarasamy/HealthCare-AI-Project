
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;

namespace Healthcare.API.Services
{
    public class ProviderService : IProviderService
    {
        private readonly HealthcareDbContext _context;

        public ProviderService(HealthcareDbContext context)
        {
            _context = context;
        }

        public List<ProviderResponseDto> GetAllProviders()
        {
            return _context.Providers
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
        }

        public ProviderResponseDto? GetProviderById(int id)
        {
            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
                return null;

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

        public ProviderResponseDto CreateProvider(ProviderCreateDto providerDto)
        {
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

        public bool UpdateProvider(int id, ProviderCreateDto providerDto)
        {
            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
                return false;

            provider.FirstName = providerDto.FirstName;
            provider.LastName = providerDto.LastName;
            provider.Specialization = providerDto.Specialization;
            provider.Email = providerDto.Email;
            provider.PhoneNumber = providerDto.PhoneNumber;
            provider.YearsOfExperience = providerDto.YearsOfExperience;

            _context.SaveChanges();

            return true;
        }

        public bool DeleteProvider(int id)
        {
            var provider = _context.Providers
                .FirstOrDefault(p => p.Id == id);

            if (provider == null)
                return false;

            _context.Providers.Remove(provider);
            _context.SaveChanges();

            return true;
        }
    }
}
