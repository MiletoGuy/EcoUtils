namespace EcoUtils.Models;

public class EcoInstance
{
    public Guid   Id              { get; set; }
    public string Apelido         { get; set; } = string.Empty;
    public string ExecutavelPath  { get; set; } = string.Empty;
    public string ExecutavelNome  { get; set; } = string.Empty;
    public string BasePath        { get; set; } = string.Empty;
    public string BaseNome        { get; set; } = string.Empty;
    public string IniPath         { get; set; } = string.Empty;
}
