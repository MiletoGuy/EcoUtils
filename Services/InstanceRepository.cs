using System.IO;
using System.Text.Json;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class InstanceRepository : IInstanceRepository
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogService _log;

    public InstanceRepository(ILogService log) => _log = log;

    private static string ArquivoPath =>
        Path.Combine(EcoPathConstants.AppDataDir, "instancias.json");

    public async Task<IReadOnlyList<EcoInstance>> CarregarAsync()
    {
        var arquivo = ArquivoPath;

        if (!File.Exists(arquivo))
            return Array.Empty<EcoInstance>();

        try
        {
            await using var stream = File.OpenRead(arquivo);
            var lista = await JsonSerializer.DeserializeAsync<List<EcoInstance>>(stream, _jsonOptions);
            return lista ?? new List<EcoInstance>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _log.Error(nameof(InstanceRepository) + "." + nameof(CarregarAsync), ex);
            return Array.Empty<EcoInstance>();
        }
    }

    public async Task SalvarAsync(IReadOnlyList<EcoInstance> instancias)
    {
        var arquivo = ArquivoPath;
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);

        await using var stream = File.Create(arquivo);
        await JsonSerializer.SerializeAsync(stream, instancias, _jsonOptions);
    }
}
