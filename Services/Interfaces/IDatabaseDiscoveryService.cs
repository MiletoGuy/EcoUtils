using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IDatabaseDiscoveryService
{
    Task<IReadOnlyList<EcoDatabase>> ListarBancosAsync();
}
