using System.Text.Json;

namespace SmartSolar.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    // Store the next delegate in the pipeline and the logger used for E5000 cases.
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Run the rest of the pipeline and translate any escaping exception into the
    // documented error envelope rather than letting a bare 500 reach a client.
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await WriteEnvelope(context, ex.StatusCode, ex.Code, ex.Message, ex.Field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteEnvelope(context, 500, "E5000", "An unexpected error occurred.", null);
        }
    }

    // Serialise the error envelope: code, safe message, optional field, trace id.
    private static async Task WriteEnvelope(HttpContext context, int status, string code, string message, string? field)
    {
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            code,
            message,
            field,
            traceId = context.TraceIdentifier
        });

        await context.Response.WriteAsync(payload);
    }
}
