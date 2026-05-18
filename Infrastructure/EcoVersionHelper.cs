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
