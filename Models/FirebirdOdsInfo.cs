namespace EcoUtils.Models;

/// <summary>
/// Versão ODS (On-Disk Structure) lida diretamente do cabeçalho de um arquivo .eco Firebird.
/// Não requer conexão ao servidor Firebird.
/// </summary>
/// <remarks>
/// Layout do cabeçalho (idêntico em todas as versões do Firebird, confirmado pelos static_assert
/// do arquivo src/jrd/ods.h do repositório oficial FirebirdSQL/firebird):
/// <code>
///   Offset  0: pag_type   (UCHAR)  — deve ser 0x01 (header page)
///   Offset  1: pag_flags  (UCHAR)
///   Offset  2: pag_reserved (USHORT)
///   Offset  4: pag_generation (ULONG)
///   Offset  8: pag_scn / pag_reserved2 (ULONG)
///   Offset 12: pag_pageno / pag_offset  (ULONG)
///   Offset 16: hdr_page_size   (USHORT) — tamanho da página em bytes
///   Offset 18: hdr_ods_version (USHORT) — major | 0x8000
///   Offset 20: hdr_ods_minor   (USHORT) — válido em ODS 12+ (Firebird 3+)
/// </code>
/// Mapeamento ODS → Firebird:
/// <list type="bullet">
///   <item>ODS 11.x → Firebird 2.5</item>
///   <item>ODS 12.x → Firebird 3.x</item>
///   <item>ODS 13.x → Firebird 4.x / 5.x</item>
///   <item>ODS 14.x → Firebird 6.x</item>
/// </list>
/// </remarks>
public readonly record struct FirebirdOdsInfo(int Major, int Minor)
{
    /// <summary>Descrição legível da versão do Firebird correspondente.</summary>
    public string VersaoFirebird => Major switch
    {
        11 => "2.5",
        12 => "3.0",
        13 => "4.0/5.0",
        14 => "6.0",
        _  => $"desconhecida (ODS {Major}.{Minor})",
    };

    /// <summary>
    /// True quando a base foi criada pelo Firebird 2.5 (ODS 11)
    /// e requer o gbak/gfix da versão 2.5.
    /// </summary>
    public bool RequerFerramentas25 => Major == 11;

    /// <summary>
    /// True quando a base requer o gbak/gfix do Firebird 5.0 ou superior (ODS 12+).
    /// O gbak 5.0 é compatível com ODS 12 (FB3) e ODS 13 (FB4/FB5).
    /// </summary>
    public bool RequerFerramentas50 => Major >= 12;

    public override string ToString() => $"ODS {Major}.{Minor} (Firebird {VersaoFirebird})";
}
