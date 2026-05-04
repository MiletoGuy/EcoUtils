using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IUserSettingsService
{
    UserSettings Settings { get; }
    Task SalvarAsync();
}
