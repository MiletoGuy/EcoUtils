namespace EcoUtils.Models;

public enum SqlParameterTipo { String, Int, Date }

public class SqlParameter
{
    public string Nome      { get; set; } = string.Empty;
    public SqlParameterTipo Tipo { get; set; } = SqlParameterTipo.String;
    public string Descricao { get; set; } = string.Empty;
}
