@echo off
echo ThisIsTheWaystone Plugin - Build Script
echo ======================================
echo.

echo Checking for .NET 8.0 SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET 8.0 SDK not found!
    echo Please download and install .NET 8.0 SDK from Microsoft
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found!
echo.

echo Building ThisIsTheWaystone plugin...
dotnet build ThisIsTheWaystone.csproj --configuration Release

if %errorlevel% equ 0 (
    echo.
    echo ✅ BUILD SUCCESSFUL!
    echo.
    echo The compiled plugin is located at:
    echo bin\Release\net8.0-windows\ThisIsTheWaystone.dll
    echo.
    echo You can now:
    echo 1. Copy the DLL to your ExileCore2 Plugins folder, OR
    echo 2. Use the source code with ExileCore2 directly
    echo.
) else (
    echo.
    echo ❌ BUILD FAILED!
    echo.
    echo Common solutions:
    echo - Make sure ExileCore2.dll, GameOffsets2.dll, and ItemFilterLibrary.dll
    echo   are in your ExileCore2 main directory
    echo - Check that all file paths are correct
    echo - Try running as administrator
    echo.
)

pause
