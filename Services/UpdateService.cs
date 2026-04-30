using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public record UpdateInfo(string Versao, string DownloadUrl);

public class UpdateService : IUpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/MiletoGuy/EcoUtils/releases/latest";

    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "EcoUtils-Updater" } }
    };

    public async Task<UpdateInfo?> VerificarAtualizacaoAsync()
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GithubRelease>(ApiUrl).ConfigureAwait(false);
            if (release is null) return null;

            var tagVersao = release.TagName.TrimStart('v');
            if (!Version.TryParse(tagVersao, out var versaoLatest)) return null;

            var versaoAtual = ObterVersaoAtual();
            if (versaoLatest <= versaoAtual) return null;

            var asset = release.Assets?.FirstOrDefault(
                a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (asset is null) return null;

            return new UpdateInfo(tagVersao, asset.BrowserDownloadUrl);
        }
        catch
        {
            return null;
        }
    }

    public async Task AtualizarAsync(UpdateInfo info, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var tempExe    = Path.Combine(Path.GetTempPath(), "EcoUtils-update.exe");
        var currentExe = Process.GetCurrentProcess().MainModule!.FileName;

        // Download com progresso
        using var response = await _http
            .GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0L;
        await using var fs     = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        var buffer     = new byte[81920];
        long downloaded = 0;
        int  read;
        while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            downloaded += read;
            if (total > 0)
                progress?.Report((double)downloaded / total);
        }

        await fs.FlushAsync(ct).ConfigureAwait(false);
        fs.Close();

        // Script batch que substitui o exe após o app fechar
        var scriptPath = Path.Combine(Path.GetTempPath(), "eco-update.bat");
        await File.WriteAllTextAsync(scriptPath,
            $"""
            @echo off
            timeout /t 2 /nobreak > nul
            copy /y "{tempExe}" "{currentExe}"
            start "" "{currentExe}"
            del "{tempExe}"
            del "%~f0"
            """, ct).ConfigureAwait(false);

        Process.Start(new ProcessStartInfo
        {
            FileName       = scriptPath,
            CreateNoWindow = true,
            WindowStyle    = ProcessWindowStyle.Hidden
        });

        System.Windows.Application.Current.Dispatcher.Invoke(
            System.Windows.Application.Current.Shutdown);
    }

    private static Version ObterVersaoAtual()
    {
        var infoVersao = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";

        // Remove sufixo do git hash, ex.: "1.0.0+abc1234" → "1.0.0"
        var str = infoVersao.Contains('+')
            ? infoVersao[..infoVersao.IndexOf('+')]
            : infoVersao;

        return Version.TryParse(str, out var v) ? v : new Version(0, 0, 0);
    }

    private sealed class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("assets")]
        public GithubAsset[]? Assets { get; set; }
    }

    private sealed class GithubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";
    }
}
