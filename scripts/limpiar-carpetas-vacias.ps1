# Elimina carpetas que no aportan nada a la estructura: vacias o que solo
# contienen un .gitkeep. Excluye bin/obj/.git (artefactos de build/control).
# Itera hasta estabilizar para barrer carpetas que quedan vacias al borrar hijas.
# ponytail: barrido simple en bucle; suficiente para un repo de este tamano.
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$excluir = '[\\/](bin|obj|\.git)[\\/]'
$borradas = @()

do {
    $cambio = $false
    Get-ChildItem -Path $raiz -Directory -Recurse -Force |
        Where-Object { $_.FullName -notmatch $excluir } |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object {
            $subdirs  = @(Get-ChildItem -LiteralPath $_.FullName -Directory -Force)
            $archivos = @(Get-ChildItem -LiteralPath $_.FullName -File -Force)
            $soloGitkeep = $archivos.Count -eq 1 -and $archivos[0].Name -eq '.gitkeep'

            if ($subdirs.Count -eq 0 -and ($archivos.Count -eq 0 -or $soloGitkeep)) {
                if ($soloGitkeep) {
                    git -C $raiz rm -q --cached -- $archivos[0].FullName 2>$null
                }
                Remove-Item -LiteralPath $_.FullName -Recurse -Force
                $script:borradas += $_.FullName.Substring($raiz.Length + 1)
                $cambio = $true
            }
        }
} while ($cambio)

if ($borradas.Count -eq 0) {
    Write-Host "Sin carpetas muertas. Estructura limpia."
} else {
    Write-Host "Carpetas eliminadas:" -ForegroundColor Yellow
    $borradas | ForEach-Object { Write-Host "  - $_" }
}
