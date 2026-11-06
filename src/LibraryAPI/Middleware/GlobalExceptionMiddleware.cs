using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace LibraryAPI.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
            context.Response.StatusCode = exception switch
            {
                KeyNotFoundException => 404,
                InvalidOperationException => 400,
                SqlException sqlException when sqlException.Number is >= 51000 and <= 51005 => 400,
                _ => 500
            };
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Request failed.", detail = context.Response.StatusCode == 500 ? "An unexpected server error occurred." : exception.Message, traceId = context.TraceIdentifier }));
        }
    }
}
