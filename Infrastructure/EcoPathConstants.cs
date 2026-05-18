using System.IO;

namespace EcoUtils.Infrastructure;

public static class EcoPathConstants
{
    // Configuráveis — populados a partir de appsettings.json em App.xaml.cs
    public static string WindowsDir    { get; set; } = @"C:\ecosis\windows";
    public static string DadosDir      { get; set; } = @"C:\ecosis\dados";
    public static string LogsDir       { get; set; } = @"C:\ecosis\logs";
    public static string EcoServerHost { get; set; } = "127.0.0.1";

    // Credenciais Firebird
    public static string FirebirdUser     { get; set; } = "sysdba";
    public static string FirebirdPassword { get; set; } = "masterkey";

    // Estrutura de diretórios Firebird
    public static string FirebirdBaseDir     => Path.Combine(WindowsDir, "Firebird");
    public static string Firebird25Dir       => Path.Combine(FirebirdBaseDir, "2.5");
    public static string Firebird50Dir       => Path.Combine(FirebirdBaseDir, "5.0");
    public static string Firebird25DllPadrao => Path.Combine(Firebird25Dir, "fbclient.dll");
    public static string Firebird50DllPadrao => Path.Combine(Firebird50Dir, "fbclient.dll");
    public static string FirebirdLegacyDll   => Path.Combine(WindowsDir, "fbclient.dll");

    // Derivados — sempre consistentes com os dirs base
    public static string UtilsDir     => Path.Combine(WindowsDir, "Utils");
    public static string EcoIniPadrao => Path.Combine(WindowsDir, "eco.ini");
    public static string LogPath      => Path.Combine(LogsDir, "ecoutils.log");

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoUtils");

    // Ferramentas bundled — extraídas de EmbeddedResource para AppData na inicialização
    public static string ToolsDir => Path.Combine(AppDataDir, "tools");
    public static string GbakPath => Path.Combine(ToolsDir, "gbak.exe");
    public static string GfixPath => Path.Combine(ToolsDir, "gfix.exe");
}
