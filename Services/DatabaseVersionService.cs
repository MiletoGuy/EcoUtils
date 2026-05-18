using System.IO;
using FirebirdSql.Data.FirebirdClient;
using EcoUtils.Infrastructure;
using EcoUtils.Models;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class DatabaseVersionService : IDatabaseVersionService
{
    private readonly ILogService _log;

    public DatabaseVersionService(ILogService log) => _log = log;

    public async Task<string?> ConsultarVersaoAsync(string ecoBankPath)
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource        = EcoPathConstants.EcoServerHost,
            Database          = ecoBankPath,
            UserID            = "SYSDBA",
            Password          = "masterkey",
            ConnectionTimeout = 5
        };

        try
        {
            await using var conn = new FbConnection(csb.ToString());
            await conn.OpenAsync();

            await using var cmd = new FbCommand("SELECT FIRST 1 VERSAO FROM TGERLICENCA", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _log.Error(nameof(DatabaseVersionService) + "." + nameof(ConsultarVersaoAsync), ex);
            return null;
        }
    }

    public async Task AlterarVersaoAsync(string ecoBankPath, string novaVersao)
    {
        var csb = new FbConnectionStringBuilder
        {
            DataSource        = EcoPathConstants.EcoServerHost,
            Database          = ecoBankPath,
            UserID            = "SYSDBA",
            Password          = "masterkey",
            ConnectionTimeout = 5
        };

        await using var conn = new FbConnection(csb.ToString());
        await conn.OpenAsync();

        await using var cmd = new FbCommand("UPDATE TGERLICENCA SET VERSAO = @versao", conn);
        cmd.Parameters.AddWithValue("@versao", novaVersao);
        await cmd.ExecuteNonQueryAsync();

        _log.Info(nameof(AlterarVersaoAsync), $"TGERLICENCA.VERSAO atualizado para '{novaVersao}' em '{ecoBankPath}'.");
    }

    /// <inheritdoc/>
    public async Task<FirebirdOdsInfo?> LerOdsDoCabecalhoAsync(string ecoBankPath)
    {
        // Layout do cabeçalho Firebird (válido para todas as versões, confirmado pelos
        // static_assert de src/jrd/ods.h do repositório FirebirdSQL/firebird):
        //
        //   Offset  0 : pag_type (UCHAR)   — 0x01 = header page
        //   Offset  1 : pag_flags (UCHAR)
        //   Offset  2 : pag_reserved (USHORT)
        //   Offset  4 : pag_generation (ULONG)
        //   Offset  8 : pag_scn (ULONG)
        //   Offset 12 : pag_pageno (ULONG)
        //   Offset 16 : hdr_page_size (USHORT)
        //   Offset 18 : hdr_ods_version (USHORT)  ← major | 0x8000
        //   Offset 20 : hdr_ods_minor   (USHORT)  ← válido somente para ODS 12+ (FB3+)
        try
        {
            const int BytesToRead = 24;
            var buffer = new byte[BytesToRead];

            await using var fs = new FileStream(
                ecoBankPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            int bytesRead = await fs.ReadAsync(buffer.AsMemory(0, BytesToRead));
            if (bytesRead < BytesToRead) return null;

            // Byte 0 deve ser 0x01 (tipo: página de cabeçalho)
            if (buffer[0] != 0x01) return null;

            ushort odsVersionRaw = BitConverter.ToUInt16(buffer, 18);

            // O bit 0x8000 é setado pelo Firebird como marca de validade do campo
            if ((odsVersionRaw & 0x8000) == 0) return null;

            int major = odsVersionRaw & 0x7FFF;

            // Minor está no offset 20 somente em ODS 12+ (Firebird 3+).
            // Em ODS 11 (FB2.5), offset 20 é hdr_PAGES — não relacionado à versão ODS.
            int minor = major >= 12 ? BitConverter.ToUInt16(buffer, 20) : 0;

            return new FirebirdOdsInfo(major, minor);
        }
        catch (Exception ex)
        {
            _log.Error(nameof(DatabaseVersionService) + "." + nameof(LerOdsDoCabecalhoAsync), ex);
            return null;
        }
    }
}
