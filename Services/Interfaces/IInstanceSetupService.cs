using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

/// <summary>
/// Opções de implantação/atualização de um .ini de instância ECO.
/// </summary>
public record ImplantarOpcoes(
    IniPreferencias? Preferencias,
    string           VersaoFirebird,
    string           PortaFirebird,
    string           DllFirebirdPath);

public interface IInstanceSetupService
{
    /// <summary>
    /// Copia o executável fonte para WindowsDir e gera o .ini correspondente,
    /// aplicando as opções de Firebird e os parâmetros de [PREFERENCIAS].
    /// O número sequencial do par de arquivos é determinado internamente com base
    /// nos arquivos já existentes em WindowsDir.
    /// </summary>
    Task<(string ExePath, string IniPath)> ImplantarAsync(
        string          exeFontePath,
        string          basePath,
        ImplantarOpcoes opcoes);

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
    /// Lê os parâmetros da seção [PREFERENCIAS] e FirebirdVersao= de [Eco] de um .ini implantado.
    /// Chaves ausentes assumem N / "2.5" / string vazia.
    /// </summary>
    Task<IniPreferencias> LerPreferenciasAsync(string iniPath);

    /// <summary>
    /// Atualiza cirurgicamente [preferencias], [Eco] e dados= em [windows] de um .ini implantado.
    /// </summary>
    Task AtualizarPreferenciasAsync(string iniPath, ImplantarOpcoes opcoes);

    /// <summary>
    /// Atualiza cirurgicamente apenas [Eco] e dados= em [windows].
    /// Usado na propagação de mudança de porta a partir das Configurações.
    /// </summary>
    Task AtualizarSecoesFbAsync(string iniPath, ImplantarOpcoes opcoes);
}
