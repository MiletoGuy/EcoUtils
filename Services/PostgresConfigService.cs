using FirebirdSql.Data.FirebirdClient;
using EcoUtils.Infrastructure;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class PostgresConfigService : IPostgresConfigService
{
    private const string SecaoConectPost = "[CONECT POST]";

    private readonly ILogService _log;

    public PostgresConfigService(ILogService log)
    {
        _log = log;
    }

    public async Task SobrescreverConectPostAsync(
        string ecoBankPath,
        string portaFirebird,
        string ipServidor,
        string portaServidor,
        string usuarioServidor,
        string senhaServidor,
        string? nomeBanco)
    {
        var csb = CriarConnectionString(ecoBankPath, portaFirebird);

        await using var conn = new FbConnection(csb.ToString());
        await conn.OpenAsync();

        var configuracaoAtual = string.Empty;

        await using (var cmdSelect = new FbCommand("SELECT FIRST 1 CONFIGURACAO FROM TGERCONFIGURACAO", conn))
        {
            var result = await cmdSelect.ExecuteScalarAsync();
            configuracaoAtual = result?.ToString() ?? string.Empty;
        }

        var configuracaoNova = SobrescreverSecaoConectPost(
            configuracaoAtual,
            ipServidor,
            portaServidor,
            usuarioServidor,
            senhaServidor,
            nomeBanco);

        if (string.Equals(configuracaoAtual, configuracaoNova, StringComparison.Ordinal))
            return;

        await using var cmdUpdate = new FbCommand("UPDATE TGERCONFIGURACAO SET CONFIGURACAO = @configuracao", conn);
        cmdUpdate.Parameters.AddWithValue("@configuracao", configuracaoNova);
        var linhasAfetadas = await cmdUpdate.ExecuteNonQueryAsync();

        _log.Info(
            nameof(SobrescreverConectPostAsync),
            $"TGERCONFIGURACAO.CONFIGURACAO atualizado em '{ecoBankPath}'. Linhas afetadas: {linhasAfetadas}.");
    }

    private static string SobrescreverSecaoConectPost(
        string textoConfiguracao,
        string ipServidor,
        string portaServidor,
        string usuarioServidor,
        string senhaServidor,
        string? nomeBanco)
    {
        var quebraLinha = textoConfiguracao.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var linhas = textoConfiguracao
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .ToList();

        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IP SERVIDOR"] = ipServidor,
            ["PORTA SERVIDOR"] = portaServidor,
            ["USUARIO SERVIDOR"] = usuarioServidor,
            ["SENHA SERVIDOR"] = senhaServidor,
        };

        var alterarNomeBanco = !string.IsNullOrWhiteSpace(nomeBanco);
        if (alterarNomeBanco)
            valores["NOME BANCO"] = nomeBanco!.Trim();

        var secaoIndex = EncontrarSecao(linhas, SecaoConectPost);
        if (secaoIndex < 0)
        {
            AdicionarNovaSecao(linhas, valores, alterarNomeBanco);
            return string.Join(quebraLinha, linhas);
        }

        var fimSecaoIndex = EncontrarFimSecao(linhas, secaoIndex + 1);
        var encontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = secaoIndex + 1; i < fimSecaoIndex; i++)
        {
            var linha = linhas[i].Trim();
            var idx = linha.IndexOf('=');
            if (idx <= 0) continue;

            var chave = linha.Substring(0, idx).Trim();
            if (!valores.TryGetValue(chave, out var valor)) continue;

            linhas[i] = $"{chave}={valor}";
            encontrados.Add(chave);
        }

        var faltantes = valores.Keys.Where(chave => !encontrados.Contains(chave)).ToList();
        if (faltantes.Count > 0)
        {
            for (var i = 0; i < faltantes.Count; i++)
            {
                var chave = faltantes[i];
                linhas.Insert(fimSecaoIndex + i, $"{chave}={valores[chave]}");
            }
        }

        return string.Join(quebraLinha, linhas);
    }

    private static int EncontrarSecao(List<string> linhas, string secao)
    {
        for (var i = 0; i < linhas.Count; i++)
        {
            if (linhas[i].Trim().Equals(secao, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static int EncontrarFimSecao(List<string> linhas, int inicioConteudo)
    {
        for (var i = inicioConteudo; i < linhas.Count; i++)
        {
            var linha = linhas[i].Trim();
            if (linha.StartsWith('[') && linha.EndsWith(']'))
                return i;
        }

        return linhas.Count;
    }

    private static void AdicionarNovaSecao(List<string> linhas, Dictionary<string, string> valores, bool incluirNomeBanco)
    {
        while (linhas.Count > 0 && string.IsNullOrWhiteSpace(linhas[^1]))
            linhas.RemoveAt(linhas.Count - 1);

        if (linhas.Count > 0)
            linhas.Add(string.Empty);

        linhas.Add(SecaoConectPost);
        linhas.Add($"IP SERVIDOR={valores["IP SERVIDOR"]}");
        linhas.Add($"PORTA SERVIDOR={valores["PORTA SERVIDOR"]}");
        linhas.Add($"USUARIO SERVIDOR={valores["USUARIO SERVIDOR"]}");
        linhas.Add($"SENHA SERVIDOR={valores["SENHA SERVIDOR"]}");

        if (incluirNomeBanco)
            linhas.Add($"NOME BANCO={valores["NOME BANCO"]}");
    }

    private static FbConnectionStringBuilder CriarConnectionString(string ecoBankPath, string portaFirebird)
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource = EcoPathConstants.EcoServerHost,
            Database = ecoBankPath,
            UserID = EcoPathConstants.FirebirdUser,
            Password = EcoPathConstants.FirebirdPassword,
            ConnectionTimeout = 5,
        };

        if (int.TryParse(portaFirebird, out var porta) && porta > 0)
            csb.Port = porta;

        return csb;
    }
}
