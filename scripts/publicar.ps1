# Publica el Sistema de Aranceles como aplicacion autocontenida para Windows x64
# y arma el paquete listo para llevar a otra PC.
#
# Resultado:
#   publish\                      -> la aplicacion (exe autocontenido + configuracion + instalar.bat)
#   SistemaAranceles-win64.zip    -> el mismo contenido comprimido, listo para copiar/enviar
#
# En la PC destino: descomprimir el zip y ejecutar instalar.bat (copia la app a una
# carpeta del usuario y crea acceso directo en el Escritorio). No requiere instalar .NET.
#
# Uso:  powershell -ExecutionPolicy Bypass -File scripts\publicar.ps1
# Nota: el empaquetado single-file tarda 1-3 minutos; no cancelar aunque parezca detenido.

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raiz "src\Presentation\SistemaAranceles.Presentation.csproj"
$salida = Join-Path $raiz "publish"
$zip = Join-Path $raiz "SistemaAranceles-win64.zip"

Write-Host "Publicando $proyecto" -ForegroundColor Cyan
Write-Host "(el empaquetado puede tardar varios minutos; no cancelar)" -ForegroundColor Yellow

if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }

dotnet publish $proyecto `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $salida

if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo (codigo $LASTEXITCODE)." }

Copy-Item (Join-Path $PSScriptRoot "instalar.bat") $salida -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $salida "*") -DestinationPath $zip

Write-Host ""
Write-Host "Listo. Contenido de ${salida}:" -ForegroundColor Green
Get-ChildItem $salida | Select-Object Name, @{N = "Tamano (MB)"; E = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table -AutoSize
Write-Host "Paquete para otra PC: $zip" -ForegroundColor Green
Write-Host "En la PC destino: descomprimir y ejecutar instalar.bat (crea acceso directo en el Escritorio)." -ForegroundColor Green
Write-Host "La cadena de conexion va en appsettings.Local.json (ver docs\DISTRIBUCION.md)." -ForegroundColor Yellow
