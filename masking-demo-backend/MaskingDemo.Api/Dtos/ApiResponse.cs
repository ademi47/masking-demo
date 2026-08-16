namespace MaskingDemo.Api.Dtos;

/// <summary>Standard EdgeCodersHub response envelope.</summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public string TraceId { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(T data, string traceId) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(string error, string traceId) =>
        new() { Success = false, Error = error, TraceId = traceId };
}
