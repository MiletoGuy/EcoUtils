using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IDatabaseDiscoveryService
{
    Task<CollectionLoadResult<EcoDatabase>> ListarBancosAsync();
}
