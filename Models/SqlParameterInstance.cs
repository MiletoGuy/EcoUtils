using System.Globalization;
using EcoUtils.ViewModels;

namespace EcoUtils.Models;

/// <summary>
/// Representa uma instância de um parâmetro de SqlEntry com o valor informado pelo usuário.
/// Estende ViewModelBase para suportar binding bidirecional do ValorTexto.
/// </summary>
public class SqlParameterInstance : ViewModelBase
{
    public SqlParameter Definicao { get; }

    private string _valorTexto = string.Empty;
    public string ValorTexto
    {
        get => _valorTexto;
        set => SetProperty(ref _valorTexto, value);
    }

    public SqlParameterInstance(SqlParameter definicao)
    {
        Definicao = definicao;
    }

    /// <summary>
    /// Tenta converter ValorTexto para o tipo esperado pelo parâmetro.
    /// Retorna (ok=true, erro=null, valor) em caso de sucesso.
    /// </summary>
    public (bool Ok, string? Erro, object? Valor) TentarConverter()
    {
        if (string.IsNullOrWhiteSpace(_valorTexto))
            return (false, $"O parâmetro '{Definicao.Nome}' é obrigatório.", null);

        return Definicao.Tipo switch
        {
            SqlParameterTipo.Int =>
                int.TryParse(_valorTexto.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                    ? (true, null, (object)i)
                    : (false, $"'{Definicao.Nome}' deve ser um número inteiro.", null),

            SqlParameterTipo.Date =>
                DateTime.TryParse(_valorTexto.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime d)
                    ? (true, null, (object)d)
                    : (false, $"'{Definicao.Nome}' deve ser uma data válida (ex: 31/12/2024).", null),

            _ => (true, null, (object?)_valorTexto.Trim())
        };
    }
}
