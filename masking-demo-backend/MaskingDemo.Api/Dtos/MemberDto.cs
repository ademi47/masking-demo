using MaskingDemo.Api.Masking;

namespace MaskingDemo.Api.Dtos;

/// <summary>
/// Response DTO. The [Mask] attributes are the ONLY masking-aware code in the app -
/// everything else is untouched.
/// </summary>
public sealed class MemberDto
{
    public int MemberId { get; init; }

    [Mask(MaskKind.Nric)]
    public string Nric { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    [Mask(MaskKind.YearOnly)]
    public string DateOfBirth { get; init; } = string.Empty;

    [Mask(MaskKind.Account)]
    public string AccountNumber { get; init; } = string.Empty;

    [Mask(MaskKind.Email)]
    public string Email { get; init; } = string.Empty;

    [Mask(MaskKind.Phone)]
    public string MobileNumber { get; init; } = string.Empty;

    public string MailingAddress { get; init; } = string.Empty;

    public List<ContributionDto> Contributions { get; init; } = [];
}

public sealed class ContributionDto
{
    public string Month { get; init; } = string.Empty;
    public string Employer { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal OrdinaryAccount { get; init; }
    public decimal SpecialAccount { get; init; }
}
