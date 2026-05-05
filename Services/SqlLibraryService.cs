using System.IO;
using System.Reflection;
using System.Text.Json;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class SqlLibraryService : ISqlLibraryService
{
    private const string EmbeddedResourceName = "EcoUtils.Resources.built-in-queries.json";

    private static readonly string CustomQueriesPath =
        Path.Combine(EcoPathConstants.AppDataDir, "custom-queries.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly List<SqlEntry> _builtIn;
    private List<SqlEntry> _custom;

    public SqlLibraryService()
    {
        _builtIn = CarregarBuiltIn();
        _custom  = CarregarCustom();
    }

    // ── Leitura ──────────────────────────────────────────────────────────────

    public IReadOnlyList<SqlEntry> ObterTodas()   => [.. _builtIn, .. _custom];
    public IReadOnlyList<SqlEntry> ObterBuiltIn() => _builtIn.AsReadOnly();
    public IReadOnlyList<SqlEntry> ObterCustom()  => _custom.AsReadOnly();

    // ── Escrita ──────────────────────────────────────────────────────────────

    public async Task SalvarCustomAsync(SqlEntry entry)
    {
        var idx = _custom.FindIndex(e => e.Id == entry.Id);
        if (idx >= 0)
            _custom[idx] = entry;
        else
            _custom.Add(entry);

        await PersistirCustomAsync();
    }

    public async Task RemoverCustomAsync(string id)
    {
        var removed = _custom.RemoveAll(e => e.Id == id);
        if (removed > 0)
            await PersistirCustomAsync();
    }

    public SqlEntry ForkBuiltIn(string builtInId)
    {
        var original = _builtIn.FirstOrDefault(e => e.Id == builtInId)
            ?? throw new ArgumentException($"SQL built-in '{builtInId}' não encontrada.", nameof(builtInId));

        return new SqlEntry
        {
            Id           = $"custom-{Guid.NewGuid():N}",
            Nome         = original.Nome,
            Categoria    = original.Categoria,
            Descricao    = original.Descricao,
            CorpoSql     = original.CorpoSql,
            Parametros   = original.Parametros.Select(p => new SqlParameter
            {
                Nome      = p.Nome,
                Tipo      = p.Tipo,
                Descricao = p.Descricao
            }).ToList(),
            IsBuiltIn    = false,
            OrigemForkId = original.Id
        };
    }

    // ── Persistência ─────────────────────────────────────────────────────────

    private async Task PersistirCustomAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CustomQueriesPath)!);
        var json = JsonSerializer.Serialize(_custom, JsonOpts);
        await File.WriteAllTextAsync(CustomQueriesPath, json);
    }

    // ── Carregamento ─────────────────────────────────────────────────────────

    private static List<SqlEntry> CarregarBuiltIn()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(EmbeddedResourceName);

            if (stream is null) return [];

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var entries = JsonSerializer.Deserialize<List<SqlEntry>>(json, JsonOpts) ?? [];
            foreach (var e in entries)
                e.IsBuiltIn = true;
            return entries;
        }
        catch { return []; }
    }

    private static List<SqlEntry> CarregarCustom()
    {
        try
        {
            if (!File.Exists(CustomQueriesPath)) return [];
            var json = File.ReadAllText(CustomQueriesPath);
            return JsonSerializer.Deserialize<List<SqlEntry>>(json, JsonOpts) ?? [];
        }
        catch { return []; }
    }
}
