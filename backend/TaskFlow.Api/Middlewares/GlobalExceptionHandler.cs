using Microsoft.AspNetCore.Diagnostics;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Api.Middlewares;

public record ErrorResponse(int Status, string Title, string Message, DateTime Timestamp)
{
    public static ErrorResponse Create(int status, string title, string message) =>
        new(status, title, message, DateTime.UtcNow);
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = exception switch
        {
            DomainException => ErrorResponse.Create(StatusCodes.Status400BadRequest, "Validation Error", exception.Message),
            NotFoundException => ErrorResponse.Create(StatusCodes.Status404NotFound, "Not Found", exception.Message),
            ConflictException => ErrorResponse.Create(StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ForbiddenException => ErrorResponse.Create(StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            InvalidCredentialsException => ErrorResponse.Create(StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            // Detalhes internos nunca vão para o cliente; ficam só no log.
            _ => ErrorResponse.Create(StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        if (error.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = error.Status;
        await context.Response.WriteAsJsonAsync(error, ct);

        return true;
    }
}
