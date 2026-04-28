namespace EcoUtils.Services.Interfaces;

public interface IIniGeneratorService
{
    Task<string> GerarIniAsync(string exeNome, string basePath);
    void RemoverIni(string iniPath);
}
