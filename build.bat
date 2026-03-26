@echo off
setlocal

:: WallpaperManager Build Script
:: Запуск сборки

echo ========================================
echo  WallpaperManager Build
echo ========================================
echo.

:: Проверка .NET
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found
    pause
    exit /b 1
)

:: Запуск PowerShell скрипта
powershell -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo [OK] Build completed
pause
