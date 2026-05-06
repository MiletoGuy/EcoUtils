using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoUtils.Services.Interfaces;

public interface IUpdateService
{
    string VersaoAtual { get; }
    Task<UpdateInfo?> VerificarAtualizacaoAsync();
    Task<IReadOnlyList<UpdateInfo>> ListarVersoesAsync();
    Task AtualizarAsync(UpdateInfo info, IProgress<double>? progress = null, CancellationToken ct = default);
}
