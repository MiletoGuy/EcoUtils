using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IVersionCatalogService
{
    Task<IReadOnlyList<EcoExecutavel>> ListarExecutaveisAsync();
}
