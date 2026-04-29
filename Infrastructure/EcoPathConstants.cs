using System.IO;

namespace EcoUtils.Infrastructure;

public static class EcoPathConstants
{
    // Configuráveis — populados a partir de appsettings.json em App.xaml.cs
    public static string WindowsDir    { get; set; } = @"C:\ecosis\windows";
    public static string DadosDir      { get; set; } = @"C:\ecosis\dados";
    public static string LogsDir       { get; set; } = @"C:\ecosis\logs";
    public static string EcoServerHost { get; set; } = "127.0.0.1";

    // Derivados — sempre consistentes com os dirs base
    public static string UtilsDir     => Path.Combine(WindowsDir, "Utils");
    public static string EcoIniPadrao => Path.Combine(WindowsDir, "eco.ini");
    public static string LogPath      => Path.Combine(LogsDir, "ecoutils.log");

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoUtils");
}
