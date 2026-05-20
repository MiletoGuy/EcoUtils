namespace EcoUtils.Models;

public sealed record CollectionLoadResult<T>(
    IReadOnlyList<T> Items,
    string? Message = null,
    bool HasError = false);