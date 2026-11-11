
using Healthcare.API.DTOs;

namespace Healthcare.API.Services
{
    public interface IProviderService
    {
        List<ProviderResponseDto> GetAllProviders();

        ProviderResponseDto? GetProviderById(int id);

        ProviderResponseDto CreateProvider(ProviderCreateDto providerDto);

        bool UpdateProvider(int id, ProviderCreateDto providerDto);

        bool DeleteProvider(int id);
    }
}
