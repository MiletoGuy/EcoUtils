using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface ISqlExecutionService
{
    /// <summary>Caminho do banco .eco atualmente selecionado.</summary>
    string? BancoAtivo { get; }

    /// <summary>Indica se há uma transação de escrita aberta aguardando Commit ou Rollback.</summary>
    bool TransacaoPendente { get; }

    /// <summary>Disparado sempre que <see cref="TransacaoPendente"/> muda de valor.</summary>
    event EventHandler TransacaoPendenteChanged;

    /// <summary>
    /// Testa a conexão com o banco informado sem alterar o banco ativo.
    /// Retorna <c>true</c> se a conexão foi bem-sucedida.
    /// </summary>
    Task<bool> TestarConexaoAsync(string ecoBankPath);

    /// <summary>
    /// Define o banco ativo para as próximas execuções.
    /// Lança <see cref="InvalidOperationException"/> se houver transação pendente.
    /// </summary>
    void DefinirBancoAtivo(string? ecoBankPath);

    /// <summary>
    /// Executa uma query SQL no banco ativo.
    /// SELECT executa fora de transação.
    /// Comandos de escrita (INSERT/UPDATE/DELETE/CREATE/ALTER/DROP/EXECUTE)
    /// abrem uma transação implícita caso não haja uma aberta.
    /// </summary>
    /// <param name="sql">Texto da query.</param>
    /// <param name="limiteLinhas">Máximo de linhas a retornar para SELECT. Null = sem limite.</param>
    Task<SqlExecutionResult> ExecutarAsync(string sql, int? limiteLinhas = null);

    /// <summary>
    /// Executa uma query com parâmetros nomeados (sintaxe :nome_param).
    /// Os valores são passados como FbParameter via ADO.NET, sem interpolação de string.
    /// </summary>
    /// <param name="sql">Texto da query com placeholders :nome_param.</param>
    /// <param name="parametros">Lista de (nome, valor) convertidos ao tipo correto.</param>
    /// <param name="limiteLinhas">Máximo de linhas a retornar para SELECT. Null = sem limite.</param>
    Task<SqlExecutionResult> ExecutarAsync(string sql, IReadOnlyList<(string nome, object? valor)> parametros, int? limiteLinhas = null);

    /// <summary>Confirma a transação de escrita ativa.</summary>
    Task CommitAsync();

    /// <summary>Desfaz a transação de escrita ativa.</summary>
    Task RollbackAsync();
}
