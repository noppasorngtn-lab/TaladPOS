using FluentAssertions;
using TaladPOS.Domain.Entities;
using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Domain.Tests;

public class StockGuardTests
{
    [Fact]
    public void Checkout_WhenQuantityExceedsStock_ThrowsInsufficientStockException()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 5);
        var staffId = Guid.NewGuid();

        var act = () => SalesOrder.Checkout(staffId, memberId: null, [(product, 6)]);

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void Checkout_WhenQuantityIsZero_ThrowsArgumentOutOfRangeException()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 5);
        var staffId = Guid.NewGuid();

        var act = () => SalesOrder.Checkout(staffId, memberId: null, [(product, 0)]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Checkout_WhenQuantityIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 5);
        var staffId = Guid.NewGuid();

        var act = () => SalesOrder.Checkout(staffId, memberId: null, [(product, -1)]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Checkout_WhenQuantityIsWithinStock_DecrementsProductQuantityOnHandAndComputesNetTotal()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 20);
        var staffId = Guid.NewGuid();

        var order = SalesOrder.Checkout(staffId, memberId: null, [(product, 3)]);

        product.QuantityOnHand.Should().Be(17);
        order.NetTotal.Should().Be(30m);
        order.StaffId.Should().Be(staffId);
        order.Status.Should().Be(SalesOrderStatus.Completed);
    }
}
