# run-dev.ps1 — clean, build e run com elevação de administrador

$projectRoot = Split-Path $PSScriptRoot -Parent
$project     = Join-Path $projectRoot "EcoUtils.csproj"

# Re-lança como admin se necessário
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
          ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
    Start-Process $shell `
        -ArgumentList "-NoExit", "-ExecutionPolicy Bypass", "-File `"$PSCommandPath`"" `
        -Verb RunAs
    exit
}

Set-Location $projectRoot

Write-Host "`n==> dotnet clean" -ForegroundColor Cyan
dotnet clean $project
if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: clean" -ForegroundColor Red; Read-Host; exit 1 }

Write-Host "`n==> dotnet build" -ForegroundColor Cyan
dotnet build $project
if ($LASTEXITCODE -ne 0) { Write-Host "FALHOU: build" -ForegroundColor Red; Read-Host; exit 1 }

Write-Host "`n==> dotnet run" -ForegroundColor Cyan
dotnet run --project $project
