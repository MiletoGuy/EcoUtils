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
    private static readonly (string ResourceName, string FileName)[] Tools =
    [
        ("EcoUtils.Tools.gbak25.exe",   "gbak25.exe"),
        ("EcoUtils.Tools.gfix25.exe",   "gfix25.exe"),
        ("EcoUtils.Tools.gbak50.exe",   "gbak50.exe"),
        ("EcoUtils.Tools.gfix50.exe",   "gfix50.exe"),
        ("EcoUtils.Tools.fbclient.dll", "fbclient.dll"),
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

        foreach (var (resourceName, fileName) in Tools)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            var destPath = Path.Combine(toolsDir, fileName);

            if (File.Exists(destPath) && HashesIguais(stream, destPath))
                continue;

            progress?.Report(fileName);

            stream.Position = 0;
            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fileStream);
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
