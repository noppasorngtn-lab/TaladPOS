using FluentAssertions;
using TaladPOS.Domain.Entities;
using TaladPOS.Domain.Pricing;

namespace TaladPOS.Domain.Tests;

public class PromotionPricingTests
{
    private static readonly DateOnly Today = new(2026, 9, 10);

    [Fact]
    public void Promotion_Constructor_WhenEndDateIsBeforeStartDate_ThrowsArgumentException()
    {
        var act = () => new Promotion(PromotionScope.WholeBill, 10m, Today, Today.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Promotion_Constructor_WhenPerProductScopeHasNoProductId_ThrowsArgumentException()
    {
        var act = () => new Promotion(PromotionScope.PerProduct, 10m, Today, Today, productId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Promotion_Constructor_WhenNonPerProductScopeHasProductId_ThrowsArgumentException()
    {
        var act = () => new Promotion(PromotionScope.WholeBill, 10m, Today, Today, productId: Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    public void Promotion_Constructor_WhenDiscountPercentIsOutOfRange_ThrowsArgumentOutOfRangeException(decimal discountPercent)
    {
        var act = () => new Promotion(PromotionScope.WholeBill, discountPercent, Today, Today);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_AppliesPerProductPromotion_ToMatchingLineOnly()
    {
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var lines = new[]
        {
            new PricingLine(productAId, UnitPrice: 100m, Quantity: 2),
            new PricingLine(productBId, UnitPrice: 50m, Quantity: 1),
        };
        var promotions = new[] { new Promotion(PromotionScope.PerProduct, 10m, Today, Today, productAId) };

        var result = SalesOrderPricingService.Calculate(lines, promotions, Today, memberLinked: false);

        result.SubtotalAmount.Should().Be(250m);
        result.PromotionDiscountAmount.Should().Be(20m); // 10% of 200 (product A only)
        result.MemberDiscountAmount.Should().Be(0m);
        result.NetTotal.Should().Be(230m);
        result.LineDiscountsByProductId[productAId].Should().Be(20m);
        result.LineDiscountsByProductId[productBId].Should().Be(0m);
    }

    [Fact]
    public void Calculate_AppliesWholeBillPromotion_ToEntireSubtotal()
    {
        var lines = new[] { new PricingLine(Guid.NewGuid(), UnitPrice: 100m, Quantity: 2) };
        var promotions = new[] { new Promotion(PromotionScope.WholeBill, 10m, Today, Today) };

        var result = SalesOrderPricingService.Calculate(lines, promotions, Today, memberLinked: false);

        result.SubtotalAmount.Should().Be(200m);
        result.PromotionDiscountAmount.Should().Be(20m);
        result.NetTotal.Should().Be(180m);
    }

    [Fact]
    public void Calculate_AppliesMemberDiscountToRemainderAfterPromotion_WhenMemberIsLinked()
    {
        var productId = Guid.NewGuid();
        var lines = new[] { new PricingLine(productId, UnitPrice: 100m, Quantity: 2) };
        var promotions = new[]
        {
            new Promotion(PromotionScope.PerProduct, 50m, Today, Today, productId),
            new Promotion(PromotionScope.MemberDiscount, 10m, Today, Today),
        };

        var result = SalesOrderPricingService.Calculate(lines, promotions, Today, memberLinked: true);

        // FR-036: promotion first (200 * 50% = 100 off -> 100 remaining), then member discount
        // on the remainder (100 * 10% = 10 off), not on the original subtotal.
        result.PromotionDiscountAmount.Should().Be(100m);
        result.MemberDiscountAmount.Should().Be(10m);
        result.NetTotal.Should().Be(90m);
    }

    [Fact]
    public void Calculate_DoesNotApplyMemberDiscount_WhenNoMemberIsLinked()
    {
        var lines = new[] { new PricingLine(Guid.NewGuid(), UnitPrice: 100m, Quantity: 1) };
        var promotions = new[] { new Promotion(PromotionScope.MemberDiscount, 10m, Today, Today) };

        var result = SalesOrderPricingService.Calculate(lines, promotions, Today, memberLinked: false);

        result.MemberDiscountAmount.Should().Be(0m);
        result.NetTotal.Should().Be(100m);
    }

    [Fact]
    public void Calculate_IgnoresPromotionOutsideItsDateWindow()
    {
        var productId = Guid.NewGuid();
        var lines = new[] { new PricingLine(productId, UnitPrice: 100m, Quantity: 1) };
        var futurePromotion = new Promotion(PromotionScope.PerProduct, 50m, Today.AddDays(1), Today.AddDays(5), productId);

        var result = SalesOrderPricingService.Calculate(lines, [futurePromotion], Today, memberLinked: false);

        result.PromotionDiscountAmount.Should().Be(0m);
        result.NetTotal.Should().Be(100m);
    }

    [Fact]
    public void Calculate_ClampsNetTotalAtZero_WhenCombinedDiscountsExceedSubtotal()
    {
        var productId = Guid.NewGuid();
        var lines = new[] { new PricingLine(productId, UnitPrice: 100m, Quantity: 1) };
        var promotions = new[]
        {
            new Promotion(PromotionScope.PerProduct, 100m, Today, Today, productId),
            new Promotion(PromotionScope.WholeBill, 100m, Today, Today),
        };

        var result = SalesOrderPricingService.Calculate(lines, promotions, Today, memberLinked: false);

        result.NetTotal.Should().Be(0m);
    }
}
