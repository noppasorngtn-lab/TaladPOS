namespace TaladPOS.Domain.Entities;

/// <summary>A time-boxed percentage discount rule (spec.md Key Entities: Promotion).</summary>
public class Promotion
{
    public Guid Id { get; private set; }
    public PromotionScope Scope { get; private set; }
    public Guid? ProductId { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsActive { get; private set; }

    private Promotion()
    {
    }

    public Promotion(PromotionScope scope, decimal discountPercent, DateOnly startDate, DateOnly endDate, Guid? productId = null)
    {
        ValidateInvariants(scope, discountPercent, startDate, endDate, productId);

        Id = Guid.NewGuid();
        Scope = scope;
        ProductId = productId;
        DiscountPercent = discountPercent;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
    }

    /// <summary>Whether this promotion is in effect on the given date (FR-023).</summary>
    public bool CoversDate(DateOnly date) => IsActive && date >= StartDate && date <= EndDate;

    /// <summary>Updates the discount rule in place (FR-020–FR-022) without touching past sales' recorded discounts.</summary>
    public void Edit(PromotionScope scope, decimal discountPercent, DateOnly startDate, DateOnly endDate, Guid? productId)
    {
        ValidateInvariants(scope, discountPercent, startDate, endDate, productId);

        Scope = scope;
        ProductId = productId;
        DiscountPercent = discountPercent;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Deactivate() => IsActive = false;

    private static void ValidateInvariants(PromotionScope scope, decimal discountPercent, DateOnly startDate, DateOnly endDate, Guid? productId)
    {
        if (scope == PromotionScope.PerProduct && productId is null)
        {
            throw new ArgumentException("ProductId is required when Scope is PerProduct.", nameof(productId));
        }

        if (scope != PromotionScope.PerProduct && productId is not null)
        {
            throw new ArgumentException("ProductId must be null unless Scope is PerProduct.", nameof(productId));
        }

        if (discountPercent <= 0 || discountPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountPercent), discountPercent, "DiscountPercent must be greater than 0 and at most 100.");
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("EndDate must be on or after StartDate.", nameof(endDate));
        }
    }
}
