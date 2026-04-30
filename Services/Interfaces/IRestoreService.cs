using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services.Interfaces;

public interface IRestoreService
{
    Task RestaurarAsync(
        string arquivoBackup,
        string destinoEco,
        IProgress<DatabaseImportProgress> progresso,
        CancellationToken ct = default);
}
