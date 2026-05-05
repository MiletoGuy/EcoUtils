namespace EcoUtils.Models;

public class SqlExecutionResult
{
    public bool Sucesso { get; init; }
    public string? Erro { get; init; }
    public IReadOnlyList<string> Colunas { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<object?>> Linhas { get; init; } = [];
    public bool LimiteAtingido { get; init; }
    public TimeSpan TempoExecucao { get; init; }
    public int LinhasAfetadas { get; init; }

    public int TotalLinhas => Linhas.Count;

    public static SqlExecutionResult Falha(string erro, TimeSpan tempo) =>
        new() { Sucesso = false, Erro = erro, TempoExecucao = tempo };
}
