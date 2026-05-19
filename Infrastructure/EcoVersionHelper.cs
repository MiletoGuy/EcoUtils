using System.Linq;

namespace EcoUtils.Infrastructure;

/// <summary>
/// Helpers for parsing and formatting Eco version strings stored in TGERLICENCA.VERSAO.
/// Format: "MMVVVPPP" where MM = 2-digit major (e.g. "14" = 1.4, "15" = 1.5),
/// VVV = version digits, PPP = 3-digit patch.
/// Examples: "14650000" → 1.4.650, "15001000" → 1.5.001
/// </summary>
public static class EcoVersionHelper
{
    public const string MajorLegadoPadrao = "14";

    /// <summary>
    /// Extracts the 2-character major prefix from a raw DB version string.
    /// "14650000" → "14", "15001000" → "15"
    /// </summary>
    public static string? ExtrairMajor(string raw)
    {
        if (raw.Length > 5 && raw.All(char.IsDigit))
            return raw.Substring(0, 2);
        return null;
    }

    /// <summary>
    /// Extracts the version number from a raw DB version string (ignores major prefix and patch suffix).
    /// "14650000" → "650", "15001000" → "001"
    /// </summary>
    public static string? ExtrairVersao(string raw)
    {
        if (raw.Length > 5 && raw.All(char.IsDigit))
            return raw.Substring(2, raw.Length - 5);
        return null;
    }

    /// <summary>
    /// Formats a raw DB version string for human-readable display.
    /// "14650000" → "1.4.650", "15001000" → "1.5.001"
    /// Returns null if the format is not recognized.
    /// </summary>
    public static string? FormatarVersao(string raw)
    {
        if (raw.Length > 5 && raw.All(char.IsDigit))
        {
            string major = raw.Substring(0, 2);                   // "14"
            string ver   = raw.Substring(2, raw.Length - 5);      // "650"
            // "14" → "1.4", "15" → "1.5"
            string majorFormatado = major.Length == 2
                ? $"{major[0]}.{major[1]}"
                : major;
            return $"{majorFormatado}.{ver}";                      // "1.4.650"
        }
        return null;
    }

    /// <summary>
    /// Normalizes an EXE version segment for comparison against the extracted DB version.
    /// Supports both legacy naming (e.g., "650" for Eco 1.4) and the newer naming convention
    /// where the major prefix is embedded in the EXE version segment (e.g., "15001" for Eco 1.5.001).
    /// If <paramref name="versaoExe"/> starts with <paramref name="majorBanco"/>, the prefix is stripped.
    /// Examples:
    ///   "650"  with major "14" → "650"   (old-style: no prefix to strip)
    ///   "15001" with major "15" → "001"  (new-style: strip "15" prefix)
    /// </summary>
    public static string NormalizarVersaoExe(string versaoExe, string? majorBanco)
    {
        if (majorBanco is not null && versaoExe.StartsWith(majorBanco, System.StringComparison.Ordinal))
            return versaoExe.Substring(majorBanco.Length);
        return versaoExe;
    }

    /// <summary>
    /// Extracts the executable version segment from names like "Eco_650_10" or "Eco_15001_10".
    /// Returns null when the name does not follow "Eco_{versao}_{build}".
    /// </summary>
    public static string? ExtrairSegmentoVersaoExe(string nomeCompleto)
    {
        var partes = nomeCompleto.Split('_');
        return partes.Length >= 2 ? partes[1] : null;
    }

    /// <summary>
    /// Detects whether the executable version segment carries an embedded major prefix.
    /// Convention: at least 5 digits and fully numeric (e.g., "15001" => major "15" + version "001").
    /// </summary>
    public static bool TemMajorEmbutidoNoSegmentoExe(string segmentoVersaoExe)
        => segmentoVersaoExe.Length >= 5 && segmentoVersaoExe.All(char.IsDigit);

    /// <summary>
    /// Extracts executable major from the version segment.
    /// Legacy names without embedded major are treated as major "14".
    /// </summary>
    public static string? ExtrairMajorExe(string nomeCompleto)
    {
        var segmento = ExtrairSegmentoVersaoExe(nomeCompleto);
        if (string.IsNullOrWhiteSpace(segmento)) return null;

        if (TemMajorEmbutidoNoSegmentoExe(segmento))
            return segmento.Substring(0, 2);

        return MajorLegadoPadrao;
    }

    /// <summary>
    /// Extracts executable version digits without major prefix.
    /// "Eco_650_10" -> "650" (legacy)
    /// "Eco_15001_10" -> "001" (major-embedded)
    /// </summary>
    public static string? ExtrairVersaoExeSemMajor(string nomeCompleto)
    {
        var segmento = ExtrairSegmentoVersaoExe(nomeCompleto);
        if (string.IsNullOrWhiteSpace(segmento)) return null;

        if (TemMajorEmbutidoNoSegmentoExe(segmento))
            return segmento.Substring(2);

        return segmento;
    }

    /// <summary>
    /// Formats compact major code to display format.
    /// "14" -> "1.4", "15" -> "1.5".
    /// </summary>
    public static string FormatarMajor(string major)
    {
        if (major.Length == 2 && major.All(char.IsDigit))
            return $"{major[0]}.{major[1]}";
        return major;
    }

    /// <summary>
    /// Normalizes user input major to compact format.
    /// Accepts "1.4" or "14" and returns "14".
    /// Returns null for unsupported formats.
    /// </summary>
    public static string? NormalizarMajorInput(string majorInput)
    {
        var texto = majorInput.Trim();
        if (texto.Length == 2 && texto.All(char.IsDigit))
            return texto;

        if (texto.Length == 3 && texto[1] == '.' && char.IsDigit(texto[0]) && char.IsDigit(texto[2]))
            return $"{texto[0]}{texto[2]}";

        return null;
    }

    /// <summary>
    /// Builds executable version segment with explicit major.
    /// major "15" + version "001" => "15001"
    /// major "14" + version "650" => "14650"
    /// </summary>
    public static string? ConstruirSegmentoVersaoExe(string major, string versao)
    {
        if (string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(versao)) return null;
        var majorNormalizado = NormalizarMajorInput(major);
        if (majorNormalizado is null) return null;

        var v = versao.Trim();
        return v.StartsWith(majorNormalizado, System.StringComparison.Ordinal)
            ? v
            : majorNormalizado + v;
    }

    /// <summary>
    /// Constructs the DB version string to write when forcing the EXE version onto the database.
    /// Preserves the original major prefix and patch suffix; replaces only the version digits.
    /// Handles both legacy EXE version segments ("651") and major-prefixed ones ("15651").
    /// "14650000" + "651"   → "14651000"
    /// "15001000" + "15002" → "15002000"
    /// </summary>
    public static string? ConstruirVersaoDBComExe(string versaoBancoRaw, string versaoExe)
    {
        if (versaoBancoRaw.Length > 5 && versaoBancoRaw.All(char.IsDigit))
        {
            int    midLen = versaoBancoRaw.Length - 5;
            string prefix = versaoBancoRaw.Substring(0, 2);
            string suffix = versaoBancoRaw.Substring(versaoBancoRaw.Length - 3);

            // Strip major prefix from EXE version if present (e.g. "15001" → "001")
            string versaoNormalizada = NormalizarVersaoExe(versaoExe, prefix);

            return prefix + versaoNormalizada.PadLeft(midLen, '0') + suffix;
        }
        return null;
    }
}
