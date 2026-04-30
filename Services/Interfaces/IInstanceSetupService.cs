using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IInstanceSetupService
{
    /// <summary>
    /// Copia o executável fonte para WindowsDir e gera o .ini correspondente,
    /// aplicando os parâmetros de <paramref name="preferencias"/> na seção [PREFERENCIAS].
    /// O número sequencial do par de arquivos é determinado internamente com base
    /// nos arquivos já existentes em WindowsDir.
    /// </summary>
    Task<(string ExePath, string IniPath)> ImplantarAsync(
        string exeFontePath,
        string basePath,
        IniPreferencias preferencias);

    /// <summary>
    /// Remove os arquivos .exe e .ini de uma instância implantada.
    /// Operação segura: valida o padrão do nome antes de deletar.
    /// </summary>
    void Remover(string exePath, string iniPath);

    /// <summary>
    /// Verifica se o eco.ini padrão existe e contém a chave 'dados=' na seção [windows].
    /// </summary>
    Task<bool> ValidarEcoIniAsync();

    /// <summary>
    /// Lê os parâmetros da seção [PREFERENCIAS] de um .ini já implantado.
    /// Chaves ausentes assumem N / string vazia.
    /// </summary>
    Task<IniPreferencias> LerPreferenciasAsync(string iniPath);

    /// <summary>
    /// Reescreve os parâmetros da seção [PREFERENCIAS] de um .ini já implantado
    /// sem alterar nenhuma outra seção.
    /// </summary>
    Task AtualizarPreferenciasAsync(string iniPath, IniPreferencias preferencias);
}
