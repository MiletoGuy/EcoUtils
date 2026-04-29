# run-dev.ps1 — clean, build e run com elevação de administrador
# Uso:
#   .\run-dev.ps1           → clean + build + run (modo padrão)
#   .\run-dev.ps1 -Publish  → gera executável portátil em .\publish\

param(
    [switch]$Publish
)

$projectRoot = Split-Path $PSScriptRoot -Parent
$project     = Join-Path $projectRoot "EcoUtils.csproj"

# Re-lança como admin se necessário (passando o parâmetro adiante)
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
          ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
    $args  = @("-NoExit", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"")
    if ($Publish) { $args += "-Publish" }
    Start-Process $shell -ArgumentList $args -Verb RunAs
    exit
}

Set-Location $projectRoot

if ($Publish) {
    $publishDir = Join-Path $projectRoot "publish"
    Write-Host "`n==> dotnet publish (single-file, self-contained)" -ForegroundColor Cyan
    dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: publish" -ForegroundColor Red; Read-Host; exit 1 }
    Write-Host "`nExecutável gerado em: $publishDir\EcoUtils.exe" -ForegroundColor Green
    exit 0
}

Write-Host "`n==> dotnet clean" -ForegroundColor Cyan
dotnet clean $project
if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: clean" -ForegroundColor Red; Read-Host; exit 1 }

Write-Host "`n==> dotnet build" -ForegroundColor Cyan
dotnet build $project
if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: build" -ForegroundColor Red; Read-Host; exit 1 }

Write-Host "`n==> dotnet run" -ForegroundColor Cyan
dotnet run --project $project
