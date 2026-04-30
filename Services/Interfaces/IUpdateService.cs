using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoUtils.Services.Interfaces;

public interface IUpdateService
{
    Task<UpdateInfo?> VerificarAtualizacaoAsync();
    Task AtualizarAsync(UpdateInfo info, IProgress<double>? progress = null, CancellationToken ct = default);
}
