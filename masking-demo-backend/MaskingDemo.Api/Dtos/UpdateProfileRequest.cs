namespace MaskingDemo.Api.Dtos;

/// <summary>
/// SAFE request DTO - deliberately has no Nric or AccountNumber property.
/// Identity comes from the token server-side, never from the request body.
/// </summary>
public sealed class UpdateProfileRequest
{
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string MailingAddress { get; init; } = string.Empty;
}

/// <summary>
/// UNSAFE request DTO - exists only to demonstrate the round-trip failure mode
/// where a client PUTs back the DTO it received and writes "S****567D" to the database.
/// Do not copy this shape into real code.
/// </summary>
public sealed class UnsafeUpdateProfileRequest
{
    public string Nric { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string MailingAddress { get; init; } = string.Empty;
}
