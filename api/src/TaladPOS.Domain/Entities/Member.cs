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

    /// <summary>
    /// Phone-number uniqueness is a cross-aggregate rule, so the actual lookup lives in the
    /// repository (Application/Infrastructure); this pure function just decides what to do with
    /// the fact, keeping the rule itself unit-testable without a database (FR-016).
    /// </summary>
    public static void EnsurePhoneNumberIsAvailable(string phoneNumber, bool isPhoneNumberTakenByAnotherMember)
    {
        if (isPhoneNumberTakenByAnotherMember)
        {
            throw new ArgumentException($"Phone number '{phoneNumber}' is already registered.", nameof(phoneNumber));
        }
    }

    /// <summary>Accrues a completed sale's net total onto this member's loyalty balance (FR-018).</summary>
    public void Credit(decimal amount) => AccumulatedPurchaseTotal += amount;

    /// <summary>Reverses a prior <see cref="Credit"/> when the linked SalesOrder is voided (FR-028).</summary>
    public void ReverseCredit(decimal amount) => AccumulatedPurchaseTotal -= amount;
}
