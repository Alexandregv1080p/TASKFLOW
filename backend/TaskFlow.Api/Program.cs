using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api;
using TaskFlow.Api.Middlewares;
using TaskFlow.Application;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Em produção vem da variável de ambiente ConnectionStrings__Default.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var demoUser = new DemoCurrentUser();
builder.Services.AddSingleton<ICurrentUser>(demoUser);

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
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks().AddDbContextCheck<TaskFlowDbContext>("postgres");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

    await db.Database.MigrateAsync();
    await demoUser.EnsureSeededAsync(db);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(_ => { });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new()
{
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
});

app.Run();

// Exposto para os testes com WebApplicationFactory.
public partial class Program;
