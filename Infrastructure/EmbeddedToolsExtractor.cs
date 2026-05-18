using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace EcoUtils.Infrastructure;

/// <summary>
/// Extrai para disco (AppData) os binários embedados como EmbeddedResource
/// (gbak.exe, gfix.exe, fbclient.dll), garantindo que estejam disponíveis
/// mesmo num publish SingleFile sem pasta tools/ ao lado do executável.
/// </summary>
public static class EmbeddedToolsExtractor
{
    private sealed record ToolEntry(string FileName, params string[] ResourceNames);

    private static readonly ToolEntry[] Tools =
    [
        // Compatibilidade: aceita recursos novos (25/50) e legado (sem sufixo).
        new("gbak25.exe", "EcoUtils.Tools.gbak25.exe", "EcoUtils.Tools.gbak.exe"),
        new("gfix25.exe", "EcoUtils.Tools.gfix25.exe", "EcoUtils.Tools.gfix.exe"),
        new("gbak50.exe", "EcoUtils.Tools.gbak50.exe"),
        new("gfix50.exe", "EcoUtils.Tools.gfix50.exe"),
        new("fbclient.dll", "EcoUtils.Tools.fbclient.dll"),
    ];

    /// <summary>
    /// Extrai os tools embedados de forma assíncrona, reportando o nome de cada
    /// arquivo que está sendo escrito via <paramref name="progress"/>.
    /// Arquivos já extraídos e inalterados (verificado por SHA-256) são pulados.
    /// </summary>
    public static Task EnsureExtractedAsync(IProgress<string>? progress = null)
        => Task.Run(() => EnsureExtracted(progress));

    public static void EnsureExtracted(IProgress<string>? progress = null)
    {
        var toolsDir = EcoPathConstants.ToolsDir;
        Directory.CreateDirectory(toolsDir);

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var tool in Tools)
        {
            var stream = TryOpenResource(assembly, tool.ResourceNames);
            if (stream is null) continue;

            using (stream)
            {
                var destPath = Path.Combine(toolsDir, tool.FileName);

                if (File.Exists(destPath) && HashesIguais(stream, destPath))
                    continue;

                progress?.Report(tool.FileName);

                stream.Position = 0;
                using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.CopyTo(fileStream);
            }
        }

        // Compatibilidade com publicações antigas: cria aliases quando só o executável
        // legado existir no AppData (gbak.exe/gfix.exe).
        GarantirAliasLegado(toolsDir, "gbak.exe", "gbak25.exe");
        GarantirAliasLegado(toolsDir, "gfix.exe", "gfix25.exe");
    }

    private static Stream? TryOpenResource(Assembly assembly, params string[] resourceNames)
    {
        foreach (var resourceName in resourceNames)
        {
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
                return stream;
        }

        return null;
    }

    private static void GarantirAliasLegado(string toolsDir, string origem, string destino)
    {
        var srcPath = Path.Combine(toolsDir, origem);
        var dstPath = Path.Combine(toolsDir, destino);

        if (!File.Exists(srcPath) || File.Exists(dstPath))
            return;

        try
        {
            File.Copy(srcPath, dstPath);
        }
        catch
        {
            // Melhor esforço: se não conseguir copiar, o RestoreService ainda tenta o legado.
        }
    }

    /// <summary>
    /// Retorna <c>true</c> se o SHA-256 do <paramref name="resource"/> coincide
    /// com o do arquivo em <paramref name="destPath"/>.
    /// </summary>
    private static bool HashesIguais(Stream resource, string destPath)
    {
        try
        {
            Span<byte> hashResource = stackalloc byte[32];
            Span<byte> hashDest     = stackalloc byte[32];

            SHA256.HashData(resource, hashResource);
            resource.Position = 0;

            using var fs = new FileStream(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            SHA256.HashData(fs, hashDest);

            return hashResource.SequenceEqual(hashDest);
        }
        catch
        {
            return false;
        }
    }
}
