namespace EcoUtils.Models;

public class UserSettings
{
    public string PortaFirebird25   { get; set; } = "3050";
    public string PortaFirebird50   { get; set; } = "3051";
    public string DllFirebird25Path { get; set; } = string.Empty;
    public string DllFirebird50Path { get; set; } = string.Empty;

    public bool SobrescreverConfiguracaoPostgres { get; set; } = true;
    public string PostgresIpServidor             { get; set; } = "LOCALHOST";
    public string PostgresPortaServidor          { get; set; } = "5432";
    public string PostgresUsuarioServidor        { get; set; } = "postgres";
    public string PostgresSenhaServidor          { get; set; } = "postgres";
    public string PostgresNomeBanco              { get; set; } = string.Empty;
}
