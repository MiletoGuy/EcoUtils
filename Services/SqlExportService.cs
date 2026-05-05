using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class SqlExportService : ISqlExportService
{
    public void CopiarTsv(DataView dataView)
    {
        if (dataView.Table is null) return;

        var sb = new StringBuilder();
        var cols = dataView.Table.Columns;

        // Cabeçalho
        sb.AppendLine(string.Join("\t", cols.Cast<DataColumn>().Select(c => EscapeTsv(c.ColumnName))));

        // Linhas
        foreach (DataRowView rowView in dataView)
        {
            var cells = rowView.Row.ItemArray
                .Select(v => EscapeTsv(v is DBNull ? string.Empty : v?.ToString()));
            sb.AppendLine(string.Join("\t", cells));
        }

        Clipboard.SetText(sb.ToString());
    }

    public async Task ExportarCsvAsync(DataView dataView, string filePath)
    {
        if (dataView.Table is null) return;

        var sb = new StringBuilder();
        var cols = dataView.Table.Columns;

        // Cabeçalho
        sb.AppendLine(string.Join(",", cols.Cast<DataColumn>().Select(c => CsvQuote(c.ColumnName))));

        // Linhas
        foreach (DataRowView rowView in dataView)
        {
            var cells = rowView.Row.ItemArray
                .Select(v => CsvQuote(v is DBNull ? string.Empty : v?.ToString()));
            sb.AppendLine(string.Join(",", cells));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string EscapeTsv(string? value) =>
        value?.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ") ?? string.Empty;

    private static string CsvQuote(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
