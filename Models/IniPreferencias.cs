namespace EcoUtils.Models;

/// <summary>
/// Parâmetros editáveis da seção [PREFERENCIAS] do eco.ini de uma instância.
/// Os valores padrão refletem os defaults do formulário de nova instância.
/// Ao ler de um .ini existente, chaves ausentes assumem N / string vazia.
/// </summary>
public class IniPreferencias
{
    public string Usuario                  { get; set; } = "SUPERVISOR";
    public bool   PesquisaTotalDosProdutos { get; set; } = true;
    public bool   MonitorarTempoSelects    { get; set; } = false;
    public bool   SincronizaTabelaPreco    { get; set; } = false;
    public bool   MultiplasInstancias      { get; set; } = true;
    /// <summary>Chave Firebird= em [preferencias]. Caminho da fbclient.dll.</summary>
    public string FirebirdDllPath          { get; set; } = string.Empty;
    /// <summary>Populado por LerPreferenciasAsync ao ler FirebirdVersao= de [Eco]. Não escrito por AplicarPreferencias.</summary>
    public string VersaoFirebird           { get; set; } = "2.5";
}
