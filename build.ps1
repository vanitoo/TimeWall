# WallpaperManager Build Script
# Сборка и публикация проекта

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter()]
    [ValidateSet("fdd", "standalone", "both")]
    [string]$PublishType = "both",

    [Parameter()]
    [switch]$Clean,

    [Parameter()]
    [string]$OutputDir = "./dist"
)

$ErrorActionPreference = "Stop"
$ProjectPath = "./WallpaperManager/WallpaperManager.csproj"
$SolutionPath = "./WallpaperManager.sln"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Проверка .NET
Write-Step "Проверка .NET SDK"

$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg ".NET SDK не найден"
    exit 1
}
Write-Success ".NET SDK $dotnetVersion"

# Очистка
if ($Clean) {
    Write-Step "Очистка проекта"
    dotnet clean $SolutionPath -c $Configuration | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Очистка завершена"
    }
}

# Восстановление
Write-Step "Восстановление зависимостей"
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Ошибка восстановления"
    exit 1
}
Write-Success "Зависимости восстановлены"

# Сборка
Write-Step "Сборка проекта ($Configuration)"
dotnet build $ProjectPath -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Ошибка сборки"
    exit 1
}
Write-Success "Сборка завершена"

# Публикация
if ($PublishType -eq "fdd" -or $PublishType -eq "both") {
    Write-Step "Публикация Framework-Dependent"
    
    $fddDir = "$OutputDir/fdd"
    dotnet publish $ProjectPath -c $Configuration -o $fddDir --self-contained false
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "FDD опубликовано: $fddDir"
    } else {
        Write-ErrorMsg "Ошибка публикации FDD"
    }
}

if ($PublishType -eq "standalone" -or $PublishType -eq "both") {
    Write-Step "Публикация Self-Contained (win-x64)"
    
    $standaloneDir = "$OutputDir/win-x64"
    dotnet publish $ProjectPath -c $Configuration -r win-x64 -o $standaloneDir --self-contained true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Standalone опубликовано: $standaloneDir"
    } else {
        Write-ErrorMsg "Ошибка публикации Standalone"
    }
}

# Итог
Write-Step "Сборка завершена!"
Write-Host ""
Write-Host "Результат:" -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration"
Write-Host "  PublishType:   $PublishType"
Write-Host "  OutputDir:     $OutputDir"
Write-Host ""

# Размеры
if (Test-Path $OutputDir) {
    Get-ChildItem $OutputDir -Directory | ForEach-Object {
        $size = (Get-ChildItem $_.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum
        $sizeMB = [Math]::Round($size / 1MB, 2)
        Write-Host "  $($_.Name): $sizeMB MB"
    }
}

Write-Host ""
