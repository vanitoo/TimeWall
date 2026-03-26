# WallpaperManager Launcher
# Проверка .NET 8.0 и запуск приложения

param(
    [switch]$Silent
)

$ErrorActionPreference = "Stop"
$AppName = "WallpaperManager"
$RequiredRuntime = "Microsoft.WindowsDesktop.App"
$MinVersion = [version]"8.0.0"
$DownloadUrl = "https://dotnet.microsoft.com/download/dotnet/8.0"

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $colors = @{
        "Info"    = "White"
        "Success" = "Green"
        "Warning" = "Yellow"
        "Error"   = "Red"
    }
    Write-Host $Message -ForegroundColor $colors[$Type]
}

function Check-DotNetRuntime {
    try {
        $runtimes = dotnet --list-runtimes 2>$null
        if ($LASTEXITCODE -ne 0) { return $false }

        foreach ($line in $runtimes) {
            if ($line -match "$RequiredRuntime\s+(\d+\.\d+\.\d+)") {
                $version = [version]$matches[1]
                if ($version -ge $MinVersion) {
                    return @{
                        Found = $true
                        Version = $version
                    }
                }
            }
        }
        return @{ Found = $false; Version = $null }
    }
    catch {
        return @{ Found = $false; Version = $null }
    }
}

function Install-DotNetRuntime {
    Write-Status "Скачивание .NET 8.0 Desktop Runtime..." "Warning"
    
    try {
        # Скачиваем установщик
        $installerPath = Join-Path $env:TEMP "dotnet-desktop-8.0.exe"
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile("https://download.visualstudio.microsoft.com/download/pr/70e72354-d73c-4c56-955b-8b533e631518/8f36c2c6a2f3c6e8f3d9c0a8c6e8c8c8/dotnet-desktop-8.0.0-win-x64.exe", $installerPath)
        
        Write-Status "Запуск установщика..." "Warning"
        Start-Process -FilePath $installerPath -Wait
        
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        # Если не удалось скачать - открываем страницу
        Write-Status "Открываю страницу загрузки..." "Warning"
        Start-Process $DownloadUrl
        return $false
    }
}

# Main
if (-not $Silent) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  $AppName" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

$runtime = Check-DotNetRuntime

if ($runtime.Found) {
    Write-Status "[OK] .NET Desktop Runtime $($runtime.Version) найден" "Success"
    
    # Запуск приложения
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $exePath = Join-Path $scriptDir "WallpaperManager.exe"
    
    if (Test-Path $exePath) {
        if (-not $Silent) {
            Write-Host "Запуск приложения..." -ForegroundColor Cyan
            Write-Host ""
        }
        
        # Запускаем и ждём завершения если не silent
        if ($Silent) {
            Start-Process $exePath -WindowStyle Minimized
        } else {
            & $exePath
        }
    } else {
        Write-Status "[ОШИБКА] WallpaperManager.exe не найден" "Error"
        exit 1
    }
}
else {
    Write-Status "[ОШИБКА] .NET 8.0 Desktop Runtime не найден" "Error"
    Write-Host ""
    Write-Host "Приложение требует .NET 8.0 Desktop Runtime для работы."
    Write-Host ""
    
    $response = Read-Host "Скачать и установить .NET 8.0? (Y/N)"
    
    if ($response -eq "Y" -or $response -eq "y") {
        $success = Install-DotNetRuntime
        
        if ($success) {
            Write-Host ""
            Write-Status "Установка завершена. Перезапустите приложение." "Success"
        } else {
            Write-Host ""
            Write-Status "Пожалуйста, установите .NET 8.0 вручную и перезапустите приложение." "Warning"
        }
    }
    
    Write-Host ""
    if (-not $Silent) {
        Read-Host "Нажмите Enter для выхода"
    }
    exit 1
}
