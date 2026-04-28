using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IInstanceRepository
{
    Task<IReadOnlyList<EcoInstance>> CarregarAsync();
    Task SalvarAsync(IReadOnlyList<EcoInstance> instancias);
}
