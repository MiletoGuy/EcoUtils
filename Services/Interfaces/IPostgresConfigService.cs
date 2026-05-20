namespace EcoUtils.Services.Interfaces;

public interface IPostgresConfigService
{
    Task SobrescreverConectPostAsync(
        string ecoBankPath,
        string portaFirebird,
        string ipServidor,
        string portaServidor,
        string usuarioServidor,
        string senhaServidor,
        string? nomeBanco);
}
