using System.Diagnostics;
using FirebirdSql.Data.FirebirdClient;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class SqlExecutionService : ISqlExecutionService, IDisposable
{
    private readonly ILogService _log;

    private string? _bancoAtivo;
    private FbConnection? _conexaoAtiva;
    private FbTransaction? _transacaoAtiva;

    public string? BancoAtivo => _bancoAtivo;
    public bool TransacaoPendente => _transacaoAtiva is not null;

    public event EventHandler? TransacaoPendenteChanged;

    public SqlExecutionService(ILogService log) => _log = log;

    // ── Banco ativo ──────────────────────────────────────────────────────────

    public void DefinirBancoAtivo(string? ecoBankPath)
    {
        if (TransacaoPendente)
            throw new InvalidOperationException(
                "Há uma transação pendente. Faça Commit ou Rollback antes de trocar o banco.");

        _bancoAtivo = ecoBankPath;
    }

    // ── Teste de conexão ─────────────────────────────────────────────────────

    public async Task<bool> TestarConexaoAsync(string ecoBankPath)
    {
        try
        {
            await using var conn = new FbConnection(CriarConnectionString(ecoBankPath));
            await conn.OpenAsync();
            await using var cmd = new FbCommand("SELECT CURRENT_TIMESTAMP FROM RDB$DATABASE", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"{nameof(SqlExecutionService)}.{nameof(TestarConexaoAsync)}", ex);
            return false;
        }
    }

    // ── Execução ─────────────────────────────────────────────────────────────

    public async Task<SqlExecutionResult> ExecutarAsync(string sql, int? limiteLinhas = null)
    {
        if (string.IsNullOrWhiteSpace(_bancoAtivo))
            return SqlExecutionResult.Falha("Nenhum banco de dados selecionado.", TimeSpan.Zero);

        if (string.IsNullOrWhiteSpace(sql))
            return SqlExecutionResult.Falha("A query está vazia.", TimeSpan.Zero);

        var sw = Stopwatch.StartNew();
        try
        {
            return EhComandoEscrita(sql)
                ? await ExecutarEscritaAsync(sql, sw, null)
                : await ExecutarSelectAsync(sql, limiteLinhas, sw, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.Error($"{nameof(SqlExecutionService)}.{nameof(ExecutarAsync)}", ex);
            return SqlExecutionResult.Falha(ex.Message, sw.Elapsed);
        }
    }

    public async Task<SqlExecutionResult> ExecutarAsync(
        string sql,
        IReadOnlyList<(string nome, object? valor)> parametros,
        int? limiteLinhas = null)
    {
        if (string.IsNullOrWhiteSpace(_bancoAtivo))
            return SqlExecutionResult.Falha("Nenhum banco de dados selecionado.", TimeSpan.Zero);

        if (string.IsNullOrWhiteSpace(sql))
            return SqlExecutionResult.Falha("A query está vazia.", TimeSpan.Zero);

        var sw = Stopwatch.StartNew();
        try
        {
            return EhComandoEscrita(sql)
                ? await ExecutarEscritaAsync(sql, sw, parametros)
                : await ExecutarSelectAsync(sql, limiteLinhas, sw, parametros);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.Error($"{nameof(SqlExecutionService)}.{nameof(ExecutarAsync)}", ex);
            return SqlExecutionResult.Falha(ex.Message, sw.Elapsed);
        }
    }

    // ── Transação ────────────────────────────────────────────────────────────

    public async Task CommitAsync()
    {
        if (_transacaoAtiva is null) return;
        try
        {
            await _transacaoAtiva.CommitAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"{nameof(SqlExecutionService)}.{nameof(CommitAsync)}", ex);
            throw;
        }
        finally
        {
            await EncerrarTransacaoAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transacaoAtiva is null) return;
        try
        {
            await _transacaoAtiva.RollbackAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"{nameof(SqlExecutionService)}.{nameof(RollbackAsync)}", ex);
            throw;
        }
        finally
        {
            await EncerrarTransacaoAsync();
        }
    }

    // ── Helpers internos ─────────────────────────────────────────────────────

    private async Task<SqlExecutionResult> ExecutarSelectAsync(
        string sql,
        int? limiteLinhas,
        Stopwatch sw,
        IReadOnlyList<(string nome, object? valor)>? parametros)
    {
        await using var conn = new FbConnection(CriarConnectionString(_bancoAtivo!));
        await conn.OpenAsync();

        await using var cmd = new FbCommand(sql, conn) { CommandTimeout = 60 };
        AdicionarParametros(cmd, parametros);
        await using var reader = await cmd.ExecuteReaderAsync();

        var colunas = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.GetName(i))
            .ToList();

        var tipos = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.GetFieldType(i))
            .ToList();

        var linhas = new List<IReadOnlyList<object?>>();
        bool limiteAtingido = false;
        int limite = limiteLinhas ?? int.MaxValue;

        while (await reader.ReadAsync())
        {
            if (linhas.Count >= limite)
            {
                limiteAtingido = true;
                break;
            }

            var linha = new object?[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                linha[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            linhas.Add(linha);
        }

        sw.Stop();
        return new SqlExecutionResult
        {
            Sucesso        = true,
            Colunas        = colunas,
            TiposColunas   = tipos,
            Linhas         = linhas,
            LimiteAtingido = limiteAtingido,
            TempoExecucao  = sw.Elapsed
        };
    }

    private async Task<SqlExecutionResult> ExecutarEscritaAsync(
        string sql,
        Stopwatch sw,
        IReadOnlyList<(string nome, object? valor)>? parametros)
    {
        // Abre conexão persistente se ainda não houver uma (necessária para manter a transação viva)
        if (_conexaoAtiva is null)
        {
            _conexaoAtiva = new FbConnection(CriarConnectionString(_bancoAtivo!));
            await _conexaoAtiva.OpenAsync();
        }

        // Abre transação implícita na primeira operação de escrita
        if (_transacaoAtiva is null)
        {
            _transacaoAtiva = _conexaoAtiva.BeginTransaction();
            TransacaoPendenteChanged?.Invoke(this, EventArgs.Empty);
        }

        await using var cmd = new FbCommand(sql, _conexaoAtiva, _transacaoAtiva) { CommandTimeout = 60 };
        AdicionarParametros(cmd, parametros);
        int linhasAfetadas = await cmd.ExecuteNonQueryAsync();

        sw.Stop();
        return new SqlExecutionResult
        {
            Sucesso        = true,
            LinhasAfetadas = linhasAfetadas,
            TempoExecucao  = sw.Elapsed
        };
    }

    private async Task EncerrarTransacaoAsync()
    {
        _transacaoAtiva?.Dispose();
        _transacaoAtiva = null;

        if (_conexaoAtiva is not null)
        {
            await _conexaoAtiva.CloseAsync();
            _conexaoAtiva.Dispose();
            _conexaoAtiva = null;
        }

        TransacaoPendenteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adiciona FbParameters nomeados ao comando, se houver.
    /// </summary>
    private static void AdicionarParametros(
        FbCommand cmd,
        IReadOnlyList<(string nome, object? valor)>? parametros)
    {
        if (parametros is null || parametros.Count == 0) return;
        foreach (var (nome, valor) in parametros)
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
    }

    /// <summary>
    /// Retorna true se o primeiro token da query for um comando de escrita (DDL/DML).
    /// SELECT e outros comandos de leitura retornam false.
    /// </summary>
    private static bool EhComandoEscrita(string sql)
    {
        var span = sql.AsSpan().TrimStart();
        int end = 0;
        while (end < span.Length && !char.IsWhiteSpace(span[end])) end++;
        var token = span[..end].ToString().ToUpperInvariant();
        return token is "INSERT" or "UPDATE" or "DELETE"
                     or "CREATE" or "ALTER"  or "DROP"
                     or "EXECUTE";
    }

    private static string CriarConnectionString(string ecoBankPath) =>
        new FbConnectionStringBuilder
        {
            DataSource        = EcoPathConstants.EcoServerHost,
            Database          = ecoBankPath,
            UserID            = EcoPathConstants.FirebirdUser,
            Password          = EcoPathConstants.FirebirdPassword,
            ConnectionTimeout = 10
        }.ToString();

    public void Dispose()
    {
        _transacaoAtiva?.Dispose();
        _conexaoAtiva?.Dispose();
    }
}
