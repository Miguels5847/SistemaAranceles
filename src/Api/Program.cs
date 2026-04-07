using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró ConnectionStrings:DefaultConnection.");

builder.Services.AddInfrastructure(defaultConnection);
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<CerrarSesionUseCase>();
builder.Services.AddScoped<ListarUsuariosUseCase>();
builder.Services.AddScoped<CrearUsuarioUseCase>();
builder.Services.AddScoped<ActualizarUsuarioUseCase>();
builder.Services.AddScoped<EliminarUsuarioUseCase>();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");

app.MapGet("/", () => Results.Ok(new
{
    service = "SistemaAranceles.Api",
    status = "running"
}));

app.MapHealthChecks("/healthz");

var authGroup = app.MapGroup("/api/auth").WithTags("Auth");
authGroup.MapPost("/login", async (
    LoginRequest request,
    LoginUseCase useCase,
    CancellationToken cancellationToken) =>
{
    try
    {
        var sesion = await useCase.EjecutarAsync(request.Correo, request.Contrasena, cancellationToken);
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
    catch (TimeoutException)
    {
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
});

authGroup.MapPost("/logout", async (
    LogoutRequest request,
    CerrarSesionUseCase useCase,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.TokenSesion))
        return Results.BadRequest(new { error = "TokenSesion es obligatorio." });

    await useCase.EjecutarAsync(request.UsuarioId, request.TokenSesion, cancellationToken);
    return Results.NoContent();
});

var usuariosGroup = app.MapGroup("/api/usuarios").WithTags("Usuarios");
usuariosGroup.MapGet("", async (
    ListarUsuariosUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var usuarios = await useCase.EjecutarAsync(cancellationToken);
    return Results.Ok(usuarios);
});

usuariosGroup.MapPost("", async (
    CrearUsuarioDto request,
    CrearUsuarioUseCase useCase,
    CancellationToken cancellationToken) =>
{
    try
    {
        var nuevoId = await useCase.EjecutarAsync(request, null, cancellationToken);
        return Results.Created($"/api/usuarios/{nuevoId}", new { id = nuevoId });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

usuariosGroup.MapPut("/{id:int}", async (
    int id,
    ActualizarUsuarioDto request,
    ActualizarUsuarioUseCase useCase,
    CancellationToken cancellationToken) =>
{
    if (id != request.Id)
        return Results.BadRequest(new { error = "El id de la ruta no coincide con el id del cuerpo." });

    try
    {
        await useCase.EjecutarAsync(request, null, cancellationToken);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

usuariosGroup.MapDelete("/{id:int}", async (
    int id,
    [FromBody] EliminarUsuarioRequest request,
    EliminarUsuarioUseCase useCase,
    CancellationToken cancellationToken) =>
{
    try
    {
        await useCase.EjecutarAsync(id, request.EliminadoPorUsuarioId, cancellationToken);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

var rolesGroup = app.MapGroup("/api/roles").WithTags("Roles");
rolesGroup.MapGet("", async (
    IRepositorioRol repositorioRol,
    CancellationToken cancellationToken) =>
{
    try
    {
        var roles = await repositorioRol.ListarAsync(cancellationToken);
        var resultado = roles.Select(r => new
        {
            id = r.Id,
            nombre = r.Nombre,
            descripcion = r.Descripcion
        });

        return Results.Ok(resultado);
    }
    catch (TimeoutException)
    {
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
    catch (Exception ex)
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
    }
});

app.Run();

public sealed record LoginRequest(string Correo, string Contrasena);
public sealed record LogoutRequest(int UsuarioId, string TokenSesion);
public sealed record EliminarUsuarioRequest(int EliminadoPorUsuarioId);
