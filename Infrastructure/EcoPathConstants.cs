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

    // Ferramentas Firebird 2.5 (gbak/gfix capazes de restaurar ODS 11)
    public static string Gbak25Path => Path.Combine(ToolsDir, "gbak25.exe");
    public static string Gfix25Path => Path.Combine(ToolsDir, "gfix25.exe");

    // Ferramentas Firebird 5.0 (gbak/gfix capazes de restaurar ODS 12, 13 e 14)
    public static string Gbak50Path => Path.Combine(ToolsDir, "gbak50.exe");
    public static string Gfix50Path => Path.Combine(ToolsDir, "gfix50.exe");

    // Atalhos para compatibilidade com código existente — apontam para ferramentas FB2.5
    public static string GbakPath => Gbak25Path;
    public static string GfixPath => Gfix25Path;
}
