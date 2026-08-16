namespace MaskingDemo.Api.Masking;

/// <summary>
/// Masking strategies. Add a member here plus a branch in <see cref="Mask.Apply"/>.
/// </summary>
public enum MaskKind
{
    Nric,
    Account,
    Phone,
    Email,
    YearOnly
}
