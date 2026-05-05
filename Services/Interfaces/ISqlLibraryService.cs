using EcoUtils.Models;

namespace EcoUtils.Services.Interfaces;

public interface ISqlLibraryService
{
    /// <summary>Todas as SQLs disponíveis (built-in + custom), já mescladas.</summary>
    IReadOnlyList<SqlEntry> ObterTodas();

    /// <summary>Apenas SQLs embutidas no assembly.</summary>
    IReadOnlyList<SqlEntry> ObterBuiltIn();

    /// <summary>Apenas SQLs personalizadas do usuário.</summary>
    IReadOnlyList<SqlEntry> ObterCustom();

    /// <summary>
    /// Adiciona ou atualiza uma SQL personalizada.
    /// Se <paramref name="entry"/> tiver um Id já existente, substitui; caso contrário, insere.
    /// </summary>
    Task SalvarCustomAsync(SqlEntry entry);

    /// <summary>Remove uma SQL personalizada pelo Id.</summary>
    Task RemoverCustomAsync(string id);

    /// <summary>
    /// Cria uma cópia editável de uma SQL built-in com novo Id único,
    /// marcando <c>OrigemForkId</c> com o Id original.
    /// A cópia NÃO é salva automaticamente — chame <see cref="SalvarCustomAsync"/> para persistir.
    /// </summary>
    SqlEntry ForkBuiltIn(string builtInId);
}
