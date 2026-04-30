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
}
