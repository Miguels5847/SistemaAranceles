using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Infrastructure.DI;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPort, out var port) && port > 0)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (string.IsNullOrWhiteSpace(allowedOrigins))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            return;
        }

        var origins = allowedOrigins
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontr� ConnectionStrings:DefaultConnection.");

// Workaround: Render DNS resolves Supabase to IPv6. Use pooler with correct username format.
if (defaultConnection.Contains("db.zpdkdbonmsjqljozaczp.supabase.co", StringComparison.OrdinalIgnoreCase))
{
    const string projectRef = "zpdkdbonmsjqljozaczp";
    var connParts = defaultConnection.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var rebuiltConn = new System.Text.StringBuilder();

    foreach (var part in connParts)
    {
        var trimmed = part.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) continue;

        if (trimmed.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) == 0)
            rebuiltConn.Append("Host=aws-1-us-east-1.pooler.supabase.com;");
        else if (trimmed.IndexOf("Port=", StringComparison.OrdinalIgnoreCase) == 0)
            rebuiltConn.Append("Port=6543;");
        else if (trimmed.IndexOf("Username=", StringComparison.OrdinalIgnoreCase) == 0)
            rebuiltConn.Append($"Username=postgres.{projectRef};");
        else
            rebuiltConn.Append(trimmed + ";");
    }

    defaultConnection = rebuiltConn.ToString().TrimEnd(';');
}

// Add resilience
if (!defaultConnection.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
{
    if (!defaultConnection.EndsWith(";")) defaultConnection += ";";
    defaultConnection += "Timeout=8;CommandTimeout=12;Keepalive=30;Pooling=true;Maximum Pool Size=20;";
}

builder.Services.AddInfrastructure(defaultConnection);
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<CerrarSesionUseCase>();
builder.Services.AddScoped<ListarUsuariosUseCase>();
builder.Services.AddScoped<CrearUsuarioUseCase>();
builder.Services.AddScoped<ActualizarUsuarioUseCase>();
builder.Services.AddScoped<EliminarUsuarioUseCase>();

var app = builder.Build();
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("DefaultCors");

app.MapGet("/", () => Results.Ok(new { service = "SistemaAranceles.Api", status = "running" }));
app.MapHealthChecks("/healthz");

var authGroup = app.MapGroup("/api/auth").WithTags("Auth");
authGroup.MapPost("/login", async (LoginRequest request, LoginUseCase useCase, CancellationToken cancellationToken) =>
{
    for (var intento = 1; intento <= 2; intento++)
    {
        try
        {
            using var ctsLogin = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsLogin.CancelAfter(TimeSpan.FromSeconds(8));
            var sesion = await useCase.EjecutarAsync(request.Correo, request.Contrasena, ctsLogin.Token);
            return Results.Ok(sesion);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (TimeoutException) when (intento == 1)
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (Exception ex) when (
            intento == 1 && (
                ex.Message.Contains("reading from stream", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase)))
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (TimeoutException)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
});

authGroup.MapPost("/logout", async (LogoutRequest request, CerrarSesionUseCase useCase, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.TokenSesion)) return Results.BadRequest(new { error = "TokenSesion obligatorio." });
    await useCase.EjecutarAsync(request.UsuarioId, request.TokenSesion, cancellationToken);
    return Results.NoContent();
});

var usuariosGroup = app.MapGroup("/api/usuarios").WithTags("Usuarios");
usuariosGroup.MapGet("", async (ListarUsuariosUseCase useCase, CancellationToken cancellationToken) =>
{
    try
    {
        using var ctsUsuarios = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ctsUsuarios.CancelAfter(TimeSpan.FromSeconds(8));
        var usuarios = await useCase.EjecutarAsync(ctsUsuarios.Token);
        return Results.Ok(usuarios);
    }
    catch (TimeoutException)
    {
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

usuariosGroup.MapPost("", async (CrearUsuarioDto request, CrearUsuarioUseCase useCase, CancellationToken cancellationToken) =>
{
    for (var intento = 1; intento <= 2; intento++)
    {
        try
        {
            using var ctsCrear = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsCrear.CancelAfter(TimeSpan.FromSeconds(16));
            var nuevoId = await useCase.EjecutarAsync(request, null, ctsCrear.Token);
            return Results.Created($"/api/usuarios/{nuevoId}", new { id = nuevoId });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (OperationCanceledException) when (intento == 1)
        {
            await Task.Delay(180, cancellationToken);
        }
        catch (TimeoutException) when (intento == 1)
        {
            await Task.Delay(180, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (TimeoutException)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
});

usuariosGroup.MapPut("/{id:int}", async (int id, ActualizarUsuarioDto request, ActualizarUsuarioUseCase useCase, CancellationToken cancellationToken) =>
{
    if (id != request.Id) return Results.BadRequest(new { error = "Id mismatch." });
    try
    {
        using var ctsActualizar = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ctsActualizar.CancelAfter(TimeSpan.FromSeconds(10));
        await useCase.EjecutarAsync(request, null, ctsActualizar.Token);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (TimeoutException) { return Results.StatusCode(StatusCodes.Status504GatewayTimeout); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status500InternalServerError); }
});

usuariosGroup.MapDelete("/{id:int}", async (int id, [FromBody] EliminarUsuarioRequest request, EliminarUsuarioUseCase useCase, CancellationToken cancellationToken) =>
{
    try { await useCase.EjecutarAsync(id, request.EliminadoPorUsuarioId, cancellationToken); return Results.NoContent(); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (TimeoutException) { return Results.StatusCode(StatusCodes.Status504GatewayTimeout); }
    catch (Exception ex) { return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status500InternalServerError); }
});

var rolesGroup = app.MapGroup("/api/roles").WithTags("Roles");
rolesGroup.MapGet("", async (IRepositorioRol repositorioRol, IMemoryCache cache, CancellationToken cancellationToken) =>
{
    const string cacheKey = "roles-list-v1";
    if (cache.TryGetValue<IReadOnlyList<RolApiDto>>(cacheKey, out var rolesCache) && rolesCache is not null)
        return Results.Ok(rolesCache);

    try
    {
        IReadOnlyList<(int Id, string Nombre, string Descripcion)> roles = [];
        for (var intento = 1; intento <= 2; intento++)
        {
            try
            {
                using var ctsRoles = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                ctsRoles.CancelAfter(TimeSpan.FromSeconds(3));
                roles = await repositorioRol.ListarAsync(ctsRoles.Token);
                break;
            }
            catch (Exception ex) when (
                intento == 1 && (
                    ex is TimeoutException
                    || ex is OperationCanceledException
                    || ex.Message.Contains("reading from stream", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase)))
            {
                await Task.Delay(120, cancellationToken);
            }
        }

        var resultado = roles
            .Select(r => new RolApiDto { id = r.Id, nombre = r.Nombre, descripcion = r.Descripcion })
            .ToList();

        cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(5));
        return Results.Ok(resultado);
    }
    catch (TimeoutException) { return Results.StatusCode(StatusCodes.Status504GatewayTimeout); }
    catch (Exception ex)
    {
        if (cache.TryGetValue<IReadOnlyList<RolApiDto>>(cacheKey, out var staleRoles) && staleRoles is not null)
            return Results.Ok(staleRoles);

        return Results.Json(new { error = ex.Message, type = ex.GetType().Name }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.Run();

public sealed record LoginRequest(string Correo, string Contrasena);
public sealed record LogoutRequest(int UsuarioId, string TokenSesion);
public sealed record EliminarUsuarioRequest(int EliminadoPorUsuarioId);
public sealed class RolApiDto
{
    public int id { get; init; }
    public string nombre { get; init; } = string.Empty;
    public string descripcion { get; init; } = string.Empty;
}
