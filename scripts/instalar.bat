@echo off
rem Instala el Sistema de Aranceles en esta PC: copia la aplicacion a una carpeta
rem propia del usuario y crea un acceso directo en el Escritorio. No requiere
rem permisos de administrador.

set "DESTINO=%LOCALAPPDATA%\SistemaAranceles\App"

echo.
echo  Instalando Sistema de Aranceles en:
echo    %DESTINO%
echo.

robocopy "%~dp0." "%DESTINO%" /E /XF instalar.bat >nul
if %ERRORLEVEL% GEQ 8 (
    echo  ERROR: no se pudieron copiar los archivos.
    pause
    exit /b 1
)

powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $lnk = $ws.CreateShortcut((Join-Path $ws.SpecialFolders.Item('Desktop') 'Sistema de Aranceles.lnk')); $lnk.TargetPath = '%DESTINO%\SistemaAranceles.exe'; $lnk.WorkingDirectory = '%DESTINO%'; $lnk.Description = 'Sistema de Aranceles Universitarios'; $lnk.Save()"

echo  Listo. Se creo el acceso directo "Sistema de Aranceles" en el Escritorio.
echo  Puedes borrar esta carpeta de instalacion si lo deseas.
echo.
pause
