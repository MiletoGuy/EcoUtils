using System.Text.Json.Serialization;

namespace EcoUtils.Models;

public class SqlEntry
{
    public string Id          { get; set; } = string.Empty;
    public string Nome        { get; set; } = string.Empty;
    public string Categoria   { get; set; } = string.Empty;
    public string Descricao   { get; set; } = string.Empty;

    [JsonPropertyName("sql")]
    public string CorpoSql    { get; set; } = string.Empty;

    public List<SqlParameter> Parametros  { get; set; } = [];

    public bool   IsBuiltIn   { get; set; } = false;
    public string? OrigemForkId { get; set; } = null;
}
