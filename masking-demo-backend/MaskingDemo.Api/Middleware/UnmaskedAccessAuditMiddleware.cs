using MaskingDemo.Api.Masking;

namespace MaskingDemo.Api.Middleware;

/// <summary>
/// Every request that bypasses masking must leave an audit trail.
/// In production this writes to a tamper-evident audit sink, not the app log.
/// </summary>
public sealed class UnmaskedAccessAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnmaskedAccessAuditMiddleware> _logger;

    public UnmaskedAccessAuditMiddleware(RequestDelegate next, ILogger<UnmaskedAccessAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (MaskingPolicy.CanViewUnmasked(context))
        {
            _logger.LogWarning(
                "UNMASKED PII ACCESS | path={Path} correlationId={CorrelationId} remoteIp={RemoteIp}",
                context.Request.Path,
                context.TraceIdentifier,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        }

        await _next(context);
    }
}
