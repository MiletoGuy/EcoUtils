using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface IDatabaseVersionService
{
    /// <summary>
    /// Conecta ao banco Firebird na porta informada e retorna o valor bruto da coluna VERSAO
    /// (ex.: "14.650.000"), ou null se a consulta falhar.
    /// </summary>
    Task<string?> ConsultarVersaoAsync(string ecoBankPath, string portaFirebird);

    /// <summary>
    /// Conecta ao banco Firebird na porta informada e atualiza o campo VERSAO da TGERLICENCA
    /// com o valor informado.
    /// </summary>
    Task AlterarVersaoAsync(string ecoBankPath, string novaVersao, string portaFirebird);

    /// <summary>
    /// Lê a versão ODS diretamente dos bytes do cabeçalho do arquivo .eco, sem
    /// precisar de conexão ao servidor Firebird. Funciona mesmo com o banco offline.
    /// Retorna null se o arquivo não existir, não for legível ou não for um banco Firebird válido.
    /// </summary>
    Task<FirebirdOdsInfo?> LerOdsDoCabecalhoAsync(string ecoBankPath);
}
