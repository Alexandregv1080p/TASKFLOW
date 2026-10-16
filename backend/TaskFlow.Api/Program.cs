using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using TaskFlow.Api;
using TaskFlow.Api.Hubs;
using TaskFlow.Api.Middlewares;
using TaskFlow.Application;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Fora de Development os logs saem em JSON, uma linha por evento: cada propriedade do template
// ({ProjectId}, {UserId}...) vira um campo pesquisável em qualquer agregador de logs.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(o =>
    {
        o.IncludeScopes = true;
        o.UseUtcTimestamp = true;
        o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    });
}

// Em produção vêm de variáveis de ambiente: ConnectionStrings__Default e Jwt__Secret.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddApplication();
// Pasta dos anexos. No Docker é um volume (/app/data); nos testes, uma pasta temporária.
var storagePath = builder.Configuration["Storage:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "data", "attachments");
builder.Services.AddInfrastructure(connectionString, jwt, storagePath);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Tempo real do quadro (ver BoardHub).
builder.Services.AddSignalR();
builder.Services.AddScoped<ILiveNotifier, SignalRLiveNotifier>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // Mantém os nomes originais dos claims ("sub"), sem o mapeamento legado para URIs da Microsoft.
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = JwtTokenService.SigningKey(jwt),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        o.Events = new JwtBearerEvents
        {
            // WebSocket no navegador não envia o header Authorization: o cliente SignalR manda o token na query.
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs") && context.Request.Query["access_token"] is { Count: > 0 } token)
                    context.Token = token.ToString();

                return Task.CompletedTask;
            },
            // 401 no mesmo formato de erro do resto da API.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(ErrorResponse.Create(
                    StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required."));
            }
        };
    });

// Tudo exige autenticação por padrão; exceções são marcadas com [AllowAnonymous].
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
    {
        // Erros de binding (JSON inválido, enum desconhecido, campo ausente) no mesmo formato do handler global.
        var message = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
            ?? "Invalid request.";

        return new BadRequestObjectResult(ErrorResponse.Create(StatusCodes.Status400BadRequest, "Validation Error", message));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // Botão "Authorize" no Swagger: cole o accessToken retornado pelo login.
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    o.AddSecurityDefinition("Bearer", scheme);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

builder.Services.AddHealthChecks().AddDbContextCheck<TaskFlowDbContext>("postgres");

// Por padrão o prometheus-net só começa a escutar System.Diagnostics.Metrics no primeiro scrape,
// perdendo o que foi medido antes. Aqui a escuta começa junto com a aplicação.
Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions { SuppressMeters = true });
MeterAdapter.StartListening();

var app = builder.Build();

// Em Development sempre; em outros ambientes só se ligado por configuração (ex.: Docker Compose).
// ponytail: migrar na inicialização serve para uma instância; com várias réplicas, aplicar as migrations
// como etapa do deploy (migration bundle no CI) para evitar corrida entre instâncias.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(_ => { });

app.UseHttpsRedirection();

// Métricas HTTP (contagem, status e latência por endpoint). Depois do roteamento para ter o nome da rota.
app.UseRouting();
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");

// /metrics para o Prometheus. Com Metrics:Port definido (Docker), só responde nessa porta interna,
// que não é publicada no host nem passa pelo nginx. As métricas de negócio (TaskFlowMetrics) e as do
// Npgsql/ASP.NET, publicadas via System.Diagnostics.Metrics, são exportadas automaticamente.
var metrics = app.MapMetrics().AllowAnonymous();
if (app.Configuration.GetValue<int?>("Metrics:Port") is { } metricsPort)
    metrics.RequireHost($"*:{metricsPort}");

app.MapHealthChecks("/health", new()
{
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
}).AllowAnonymous();

app.Run();

// Exposto para os testes com WebApplicationFactory.
public partial class Program;
