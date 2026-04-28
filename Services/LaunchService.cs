using System.Diagnostics;
using System.IO;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class LaunchService : ILaunchService
{
    public async Task<(bool Sucesso, string? Erro)> ExecutarAsync(EcoInstance instancia)
    {
        if (!File.Exists(instancia.ExecutavelPath))
            return (false, $"Executável não encontrado: {instancia.ExecutavelPath}");

        if (!File.Exists(instancia.BasePath))
            return (false, $"Banco de dados não encontrado: {instancia.BasePath}");

        if (!File.Exists(instancia.IniPath))
            return (false, $"Arquivo de configuração .ini não encontrado: {instancia.IniPath}");

        await Task.Run(() =>
        {
            var info = new ProcessStartInfo
            {
                FileName        = instancia.ExecutavelPath,
                UseShellExecute = true
            };
            Process.Start(info);
        });

        return (true, null);
    }
}
