namespace EcoUtils.Models;

public class EcoExecutavel
{
    public string NomeCompleto      { get; set; } = string.Empty;  // ex.: "Eco_650_10"
    public string ExePath           { get; set; } = string.Empty;  // ex.: "C:/ecosis/windows/Eco_650_10.exe"
    public bool   IniPadraoPresente { get; set; }                  // eco.ini padrão existe na pasta?
}
