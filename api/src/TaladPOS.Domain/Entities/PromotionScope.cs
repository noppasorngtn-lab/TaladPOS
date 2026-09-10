namespace TaladPOS.Domain.Entities;

/// <summary>What a Promotion's discount applies to (FR-020, FR-021).</summary>
public enum PromotionScope
{
    PerProduct,
    WholeBill,
    MemberDiscount,
}
