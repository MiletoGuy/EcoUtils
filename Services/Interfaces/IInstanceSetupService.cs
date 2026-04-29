namespace EcoUtils.Services.Interfaces;

public interface IInstanceSetupService
{
    /// <summary>
    /// Copia o executável fonte para WindowsDir e gera o .ini correspondente.
    /// O número sequencial do par de arquivos é determinado internamente com base
    /// nos arquivos já existentes em WindowsDir.
    /// </summary>
    Task<(string ExePath, string IniPath)> ImplantarAsync(
        string exeFontePath,
        string basePath);

    /// <summary>
    /// Remove os arquivos .exe e .ini de uma instância implantada.
    /// Operação segura: valida o padrão do nome antes de deletar.
    /// </summary>
    void Remover(string exePath, string iniPath);

    /// <summary>
    /// Verifica se o eco.ini padrão existe e contém a chave 'dados=' na seção [windows].
    /// </summary>
    Task<bool> ValidarEcoIniAsync();
}
