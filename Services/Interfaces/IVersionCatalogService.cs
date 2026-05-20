using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IVersionCatalogService
{
    Task<CollectionLoadResult<EcoExecutavel>> ListarExecutaveisAsync();
}
