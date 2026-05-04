using System.IO;
using System.Reflection;

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
        ("EcoUtils.Tools.gbak.exe",     "gbak.exe"),
        ("EcoUtils.Tools.gfix.exe",     "gfix.exe"),
        ("EcoUtils.Tools.fbclient.dll", "fbclient.dll"),
    ];

    public static void EnsureExtracted()
    {
        var toolsDir = EcoPathConstants.ToolsDir;
        Directory.CreateDirectory(toolsDir);

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var (resourceName, fileName) in Tools)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            var destPath = Path.Combine(toolsDir, fileName);

            // Só regrava se o tamanho diferir — evita I/O desnecessário
            // e não interrompe um gbak em execução no meio de uma restauração.
            if (File.Exists(destPath) && new FileInfo(destPath).Length == stream.Length)
                continue;

            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fileStream);
        }
    }
}
