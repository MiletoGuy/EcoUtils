namespace EcoUtils.Infrastructure;

/// <summary>
/// POCO mapeado de appsettings.json. Apenas os diretórios base e o host são
/// configuráveis; os caminhos derivados (UtilsDir, EcoIniPadrao, LogPath)
/// são computados automaticamente por EcoPathConstants.
/// </summary>
public class EcoSettings
{
    public string WindowsDir    { get; set; } = @"C:\ecosis\windows";
    public string DadosDir      { get; set; } = @"C:\ecosis\dados";
    public string LogsDir       { get; set; } = @"C:\ecosis\logs";
    public string EcoServerHost { get; set; } = "127.0.0.1";
}
