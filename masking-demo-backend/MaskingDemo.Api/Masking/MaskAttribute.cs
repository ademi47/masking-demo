namespace MaskingDemo.Api.Masking;

/// <summary>
/// Marks a DTO property to be masked during JSON serialisation.
/// Apply to response DTOs only - never to domain entities, which must keep real values
/// for lookups, joins and audit.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaskAttribute : Attribute
{
    public MaskAttribute(MaskKind kind = MaskKind.Nric) => Kind = kind;

    public MaskKind Kind { get; }
}
