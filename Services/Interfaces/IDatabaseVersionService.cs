namespace EcoUtils.Services.Interfaces;

public interface IDatabaseVersionService
{
    /// <summary>
    /// Conecta ao banco Firebird e retorna o valor bruto da coluna VERSAO
    /// (ex.: "14.650.000"), ou null se a consulta falhar.
    /// </summary>
    Task<string?> ConsultarVersaoAsync(string ecoBankPath);
}
