namespace EcoUtils.Models;

public class EcoExecutavel
{
    public string NomeCompleto { get; set; } = string.Empty;  // ex.: "Eco_650_10"
    public string ExePath      { get; set; } = string.Empty;  // ex.: "C:\ecosis\windows\Utils\Eco_650_10.exe"

    // ex.: "10"  (terceiro segmento do NomeCompleto)
    public string NumeroBuild
    {
        get
        {
            var partes = NomeCompleto.Split('_');
            return partes.Length >= 3 ? partes[2] : NomeCompleto;
        }
    }
}
