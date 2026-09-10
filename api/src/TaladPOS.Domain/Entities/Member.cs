namespace TaladPOS.Domain.Entities;

/// <summary>A loyalty program participant (spec.md Key Entities: Member).</summary>
public class Member
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public decimal AccumulatedPurchaseTotal { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private Member()
    {
    }

    public Member(string name, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Member name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("PhoneNumber is required.", nameof(phoneNumber));
        }

        Id = Guid.NewGuid();
        Name = name;
        PhoneNumber = phoneNumber;
        AccumulatedPurchaseTotal = 0m;
        JoinedAt = DateTimeOffset.UtcNow;
    }
}
