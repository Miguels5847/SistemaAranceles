# Distribución — ejecutar el Sistema de Aranceles en otra computadora

## 1. Generar el paquete (en la máquina de desarrollo)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publicar.ps1
```

> El empaquetado single-file tarda 1–3 minutos. **No cancelar** aunque la consola parezca
> detenida en "GenerateBundle": está comprimiendo el ejecutable.

Produce dos cosas:

- `publish\` — la aplicación: `SistemaAranceles.Presentation.exe` **autocontenido** (incluye
  .NET 8, la otra PC no instala nada), `appsettings.json`/`appsettings.Local.json`, las fuentes
  PDF (`LatoFont`) y el `instalar.bat`.
- `SistemaAranceles-win64.zip` — lo mismo comprimido, listo para USB/correo/Drive.

## 2. Instalar en la otra PC (recomendado)

1. Copia el `SistemaAranceles-win64.zip` a la otra computadora y descomprímelo donde sea
   (Descargas, USB, etc.).
2. Doble clic en **`instalar.bat`**: copia la aplicación a una carpeta propia del usuario
   (`%LOCALAPPDATA%\SistemaAranceles\App`) y crea el acceso directo **"Sistema de Aranceles"**
   en el Escritorio. No pide permisos de administrador.
3. Abre la app desde el acceso directo. La carpeta descomprimida ya se puede borrar.

### Alternativa manual (sin instalador)

Copiar la carpeta `publish\` completa a cualquier ruta de la otra PC y ejecutar
`SistemaAranceles.Presentation.exe` desde ahí. **El exe solo no basta**: necesita junto a él
`appsettings.Local.json` (cadena de conexión) y la carpeta `LatoFont` (fuentes de los PDF) —
por eso se distribuye la carpeta o el zip, no el archivo suelto.

## 3. La cadena de conexión

`appsettings.Local.json` junto al exe (el publish copia el de tu máquina; verifícalo):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.<proyecto>;Password=<la-clave>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

Alternativa sin archivo: variable de entorno `SUPABASE_DB_CONNECTION` con la cadena completa
(tiene prioridad sobre los appsettings).

## 4. Requisitos de la otra PC

| Requisito | Detalle |
|---|---|
| Sistema | Windows 10/11 de 64 bits |
| .NET | **No se necesita** (va dentro del exe) |
| Red | Salida HTTPS/5432-6543 hacia `*.pooler.supabase.com` (la BD está en la nube) |
| Disco | ~250 MB |

## 5. Diagnóstico

- Logs de arranque y de errores: `%LOCALAPPDATA%\SistemaAranceles\` en la PC donde corre.
- Si la app abre pero el login falla: revisar la cadena de conexión (el log registra el
  endpoint y si la tomó de `appsettings` o de la variable de entorno, nunca la contraseña).
- El primer arranque tarda unos segundos más (extracción inicial del single-file).

## 6. Seguridad

- `appsettings.Local.json` contiene la credencial de la BD: distribuir solo a equipos de
  confianza. Para una entrega académica/demo se puede crear un usuario de BD con permisos
  mínimos en Supabase y usar esa cadena.
