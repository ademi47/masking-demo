using MaskingDemo.Api.Models;

namespace MaskingDemo.Api.Data;

/// <summary>In-memory seed data so the demo runs with no database.</summary>
public sealed class MemberStore
{
    private readonly Dictionary<int, Member> _members;

    public MemberStore()
    {
        var member = new Member
        {
            MemberId = 88214,
            Nric = "S1234567D",
            Name = "Tan Wei Ming",
            DateOfBirth = "1985-03-14",
            AccountNumber = "0123456789",
            Email = "weiming.tan@example.com",
            MobileNumber = "91234567",
            MailingAddress = "Blk 123 Ang Mo Kio Ave 6, #08-45, Singapore 560123",
            Contributions =
            [
                new Contribution { Month = "2024-01", Employer = "Acme Pte Ltd", Amount = 1840.00m, OrdinaryAccount = 1104.00m, SpecialAccount = 386.40m },
                new Contribution { Month = "2024-02", Employer = "Acme Pte Ltd", Amount = 1840.00m, OrdinaryAccount = 1104.00m, SpecialAccount = 386.40m },
                new Contribution { Month = "2024-03", Employer = "Acme Pte Ltd", Amount = 1920.00m, OrdinaryAccount = 1152.00m, SpecialAccount = 403.20m }
            ]
        };

        _members = new Dictionary<int, Member> { [member.MemberId] = member };
    }

    public Member? Find(int memberId) =>
        _members.TryGetValue(memberId, out var member) ? member : null;
}
