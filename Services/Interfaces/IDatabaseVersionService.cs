using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IDatabaseVersionService
{
    /// <summary>
    /// Conecta ao banco Firebird e retorna o valor bruto da coluna VERSAO
    /// (ex.: "14.650.000"), ou null se a consulta falhar.
    /// </summary>
    Task<string?> ConsultarVersaoAsync(string ecoBankPath);

    /// <summary>
    /// Conecta ao banco Firebird e atualiza o campo VERSAO da TGERLICENCA
    /// com o valor informado.
    /// </summary>
    Task AlterarVersaoAsync(string ecoBankPath, string novaVersao);

    /// <summary>
    /// Lê a versão ODS diretamente dos bytes do cabeçalho do arquivo .eco, sem
    /// precisar de conexão ao servidor Firebird. Funciona mesmo com o banco offline.
    /// Retorna null se o arquivo não existir, não for legível ou não for um banco Firebird válido.
    /// </summary>
    Task<FirebirdOdsInfo?> LerOdsDoCabecalhoAsync(string ecoBankPath);
}
