using EcoUtils.Models;

namespace EcoUtils.ViewModels;

/// <summary>
/// VM de edição da definição de um parâmetro (nome, tipo, descrição).
/// Usado no SqlEditorViewModel para construir/editar a lista de parâmetros de uma SqlEntry.
/// </summary>
public class SqlParameterEditItem : ViewModelBase
{
    private string _nome = string.Empty;
    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }

    private SqlParameterTipo _tipo = SqlParameterTipo.String;
    public SqlParameterTipo Tipo
    {
        get => _tipo;
        set => SetProperty(ref _tipo, value);
    }

    private string _descricao = string.Empty;
    public string Descricao
    {
        get => _descricao;
        set => SetProperty(ref _descricao, value);
    }

    public static IReadOnlyList<SqlParameterTipo> TodosTipos { get; } =
        Enum.GetValues<SqlParameterTipo>().ToList();
}
