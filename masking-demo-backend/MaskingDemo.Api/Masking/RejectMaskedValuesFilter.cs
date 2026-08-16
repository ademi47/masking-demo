using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MaskingDemo.Api.Masking;

/// <summary>
/// Write-path guard. Blocks any inbound payload containing the mask character so a
/// client that round-trips a response DTO cannot overwrite a real NRIC with asterisks.
/// This is a safety net - the primary defence is request DTOs that omit the field entirely.
/// </summary>
public sealed class RejectMaskedValuesFilter : IActionFilter
{
    private const int MaxDepth = 4;
    private readonly ILogger<RejectMaskedValuesFilter> _logger;

    public RejectMaskedValuesFilter(ILogger<RejectMaskedValuesFilter> logger) => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (name, argument) in context.ActionArguments)
        {
            if (argument is null || !ContainsMaskChar(argument, 0))
            {
                continue;
            }

            _logger.LogWarning(
                "Rejected request to {Path}: argument {Argument} contained masked values",
                context.HttpContext.Request.Path, name);

            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Masked value submitted",
                Detail = "The payload contains a masked value. Send only fields the user actually edited.",
                Status = StatusCodes.Status400BadRequest
            });

            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static bool ContainsMaskChar(object value, int depth)
    {
        if (depth > MaxDepth)
        {
            return false;
        }

        switch (value)
        {
            case string s:
                return s.Contains(Mask.MaskChar);

            case IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    if (item is not null && ContainsMaskChar(item, depth + 1))
                    {
                        return true;
                    }
                }
                return false;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime))
        {
            return false;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var child = property.GetValue(value);
            if (child is not null && ContainsMaskChar(child, depth + 1))
            {
                return true;
            }
        }

        return false;
    }
}
