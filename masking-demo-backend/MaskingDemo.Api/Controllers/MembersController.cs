using System.Text;
using MaskingDemo.Api.Data;
using MaskingDemo.Api.Dtos;
using MaskingDemo.Api.Masking;
using MaskingDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MaskingDemo.Api.Controllers;

[ApiController]
[Route("api/v1/members")]
public sealed class MembersController : ControllerBase
{
    // DEMO ONLY. In production this comes from the validated JWT "sub" claim -
    // never from a header, a route parameter or a query string.
    private const int CurrentMemberId = 88214;

    private readonly MemberStore _store;
    private readonly ILogger<MembersController> _logger;

    public MembersController(MemberStore store, ILogger<MembersController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>The real endpoint. Masking is applied by the serialiser, not by this method.</summary>
    [HttpGet("me")]
    public ActionResult<ApiResponse<MemberDto>> GetMe()
    {
        var member = _store.Find(CurrentMemberId);
        if (member is null)
        {
            return NotFound(ApiResponse<MemberDto>.Fail("Member not found", HttpContext.TraceIdentifier));
        }

        // Log with the masked value - log sinks bypass the JSON serialiser entirely.
        _logger.LogInformation(
            "Profile read | memberId={MemberId} nric={MaskedNric}",
            member.MemberId, Mask.Apply(member.Nric, MaskKind.Nric));

        return Ok(ApiResponse<MemberDto>.Ok(ToDto(member), HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// DEMO ONLY - returns the raw entity so you can show the "before" payload
    /// side by side in the network tab. Delete this before any real deployment.
    /// </summary>
    [HttpGet("me/unmasked-demo")]
    public ActionResult<ApiResponse<Member>> GetMeUnmaskedDemo()
    {
        var member = _store.Find(CurrentMemberId);
        return member is null
            ? NotFound(ApiResponse<Member>.Fail("Member not found", HttpContext.TraceIdentifier))
            : Ok(ApiResponse<Member>.Ok(member, HttpContext.TraceIdentifier));
    }

    /// <summary>CSV export - bypasses the JSON serialiser, so masking must be applied by hand.</summary>
    [HttpGet("me/export")]
    public IActionResult ExportCsv()
    {
        var member = _store.Find(CurrentMemberId);
        if (member is null)
        {
            return NotFound();
        }

        var csv = new StringBuilder()
            .AppendLine("MemberId,Nric,Name,AccountNumber,Month,Employer,Amount");

        foreach (var c in member.Contributions)
        {
            csv.AppendLine(string.Join(',',
                member.MemberId,
                Mask.Apply(member.Nric, MaskKind.Nric),
                member.Name,
                Mask.Apply(member.AccountNumber, MaskKind.Account),
                c.Month,
                c.Employer,
                c.Amount.ToString("F2")));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "contributions.csv");
    }

    /// <summary>Safe write path - request DTO has no NRIC at all.</summary>
    [HttpPut("me/profile")]
    public ActionResult<ApiResponse<MemberDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var member = _store.Find(CurrentMemberId);
        if (member is null)
        {
            return NotFound(ApiResponse<MemberDto>.Fail("Member not found", HttpContext.TraceIdentifier));
        }

        member.Email = request.Email;
        member.MobileNumber = request.MobileNumber;
        member.MailingAddress = request.MailingAddress;

        return Ok(ApiResponse<MemberDto>.Ok(ToDto(member), HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// DEMO ONLY - accepts an NRIC in the body so you can show RejectMaskedValuesFilter
    /// returning 400 when the client round-trips a masked value.
    /// </summary>
    [HttpPut("me/profile-unsafe-demo")]
    public ActionResult<ApiResponse<string>> UpdateProfileUnsafeDemo([FromBody] UnsafeUpdateProfileRequest request)
    {
        return Ok(ApiResponse<string>.Ok(
            $"Would have written NRIC '{request.Nric}' to the database.",
            HttpContext.TraceIdentifier));
    }

    private static MemberDto ToDto(Member m) => new()
    {
        MemberId = m.MemberId,
        Nric = m.Nric,
        Name = m.Name,
        DateOfBirth = m.DateOfBirth,
        AccountNumber = m.AccountNumber,
        Email = m.Email,
        MobileNumber = m.MobileNumber,
        MailingAddress = m.MailingAddress,
        Contributions = m.Contributions.Select(c => new ContributionDto
        {
            Month = c.Month,
            Employer = c.Employer,
            Amount = c.Amount,
            OrdinaryAccount = c.OrdinaryAccount,
            SpecialAccount = c.SpecialAccount
        }).ToList()
    };
}
