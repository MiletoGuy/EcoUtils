using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface ILaunchService
{
    Task<(bool Sucesso, string? Erro)> ExecutarAsync(EcoInstance instancia);
}
