using System.IO;

namespace EcoUtils.Infrastructure;

public static class EcoPathConstants
{
    public const string WindowsDir    = @"C:\ecosis\windows";
    public const string DadosDir      = @"C:\ecosis\dados";
    public const string EcoIniPadrao  = @"C:\ecosis\windows\eco.ini";

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoUtils");
}
