using System.IO;

namespace EcoUtils.Infrastructure;

public static class EcoPathConstants
{
    public const string WindowsDir   = @"C:\ecosis\windows";
    public const string UtilsDir     = @"C:\ecosis\windows\Utils";
    public const string DadosDir     = @"C:\ecosis\dados";
    public const string EcoIniPadrao = @"C:\ecosis\windows\eco.ini";
    public const string LogsDir      = @"C:\ecosis\logs";
    public const string LogPath      = @"C:\ecosis\logs\ecoutils.log";
    public const string EcoServerHost = "127.0.0.1";

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoUtils");
}
