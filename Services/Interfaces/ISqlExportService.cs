using System.Data;

namespace EcoUtils.Services.Interfaces;

public interface ISqlExportService
{
    /// <summary>Copia as linhas visíveis do DataView para o clipboard em formato TSV.</summary>
    void CopiarTsv(DataView dataView);

    /// <summary>Exporta as linhas visíveis do DataView para um arquivo CSV.</summary>
    Task ExportarCsvAsync(DataView dataView, string filePath);
}
