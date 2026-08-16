namespace MaskingDemo.Api.Models;

/// <summary>
/// Domain entity - always holds REAL values. Masking happens only at the API boundary.
/// </summary>
public sealed class Member
{
    public required int MemberId { get; init; }
    public required string Nric { get; init; }
    public required string Name { get; init; }
    public required string DateOfBirth { get; init; }
    public required string AccountNumber { get; init; }
    public required string Email { get; set; }
    public required string MobileNumber { get; set; }
    public required string MailingAddress { get; set; }
    public List<Contribution> Contributions { get; init; } = [];
}

public sealed class Contribution
{
    public required string Month { get; init; }
    public required string Employer { get; init; }
    public required decimal Amount { get; init; }
    public required decimal OrdinaryAccount { get; init; }
    public required decimal SpecialAccount { get; init; }
}
