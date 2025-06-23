@echo off
REM Clean Architecture Code Generator Wrapper Script
REM Usage: generate.bat [entity] [options]

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "GENERATOR_DIR=%SCRIPT_DIR%tools\CodeGenerator"

REM Check if generator exists
if not exist "%GENERATOR_DIR%" (
    echo ❌ Error: CodeGenerator not found at %GENERATOR_DIR%
    echo Please run this script from the project root directory.
    exit /b 1
)

REM If no arguments, list entities
if "%~1"=="" (
    echo 🔍 Listing available entities...
    dotnet run --project "%GENERATOR_DIR%"
    goto :end
)

REM Run with arguments
echo 🚀 Running Code Generator...
dotnet run --project "%GENERATOR_DIR%" -- %*

if %errorlevel% equ 0 (
    echo.
    echo ✅ Code generation completed successfully!
    echo 💡 Next steps:
    echo    1. dotnet build
    echo    2. dotnet run --project src\API
    echo    3. Open https://localhost:7049/swagger
) else (
    echo.
    echo ❌ Code generation failed!
)

:end
pause