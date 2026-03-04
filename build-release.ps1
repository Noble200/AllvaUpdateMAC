# Script de compilación y empaquetado

param(
    [string]$Version = "1.0.0"
)

Write-Host "=== Compilando AllvaSystem v$Version ===" -ForegroundColor Cyan

# Limpiar
Write-Host "Limpiando..." -ForegroundColor Yellow
dotnet clean -c Release

# Compilar
Write-Host "Compilando..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la compilación" -ForegroundColor Red
    exit 1
}

# Crear carpeta Releases
if (-not (Test-Path ".\Releases")) {
    New-Item -ItemType Directory -Path ".\Releases"
}

# Empaquetar con Velopack
Write-Host "Creando instalador..." -ForegroundColor Yellow
vpk pack `
    --packId AllvaSystem `
    --packVersion $Version `
    --packDir ".\bin\Release\net8.0\win-x64\publish" `
    --mainExe Allva.Desktop.exe `
    --packTitle "AllvaSystem" `
    --packAuthors "AllvaSystem" `
    --outputDir ".\Releases"

if ($LASTEXITCODE -eq 0) {
    Write-Host "=== ¡Instalador creado! ===" -ForegroundColor Green
    Write-Host "Archivos en: .\Releases\" -ForegroundColor Green
} else {
    Write-Host "Error creando instalador" -ForegroundColor Red
}