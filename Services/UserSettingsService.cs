using System.IO;
using System.Text.Json;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class UserSettingsService : IUserSettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(EcoPathConstants.AppDataDir, "usersettings.json");

    public UserSettings Settings { get; private set; } = new();

    public UserSettingsService()
    {
        Carregar();
    }

    private void Carregar()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json   = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<UserSettings>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (loaded is not null)
                Settings = loaded;
        }
        catch { /* usa defaults */ }
    }

    public async Task SalvarAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var json = JsonSerializer.Serialize(Settings,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsPath, json);
    }
}
