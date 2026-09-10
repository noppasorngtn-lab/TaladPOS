using FluentAssertions;
using TaladPOS.Domain.Entities;
using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Domain.Tests;

public class SalesOrderVoidTests
{
    private static SalesOrder CreateOrder(out Product product, int quantityOnHand = 20, int quantitySold = 3)
    {
        product = new Product("Mango", price: 10m, quantityOnHand: quantityOnHand);
        return SalesOrder.Checkout(Guid.NewGuid(), memberId: null, [(product, quantitySold)]);
    }

    private static Dictionary<Guid, Product> ProductsById(params Product[] products) =>
        products.ToDictionary(p => p.Id);

    [Fact]
    public void Void_OnSameCalendarDayAsCreation_TransitionsToVoided()
    {
        var order = CreateOrder(out var product);

        order.Void(order.CreatedAt, ProductsById(product));

        order.Status.Should().Be(SalesOrderStatus.Voided);
        order.VoidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Void_OnADifferentCalendarDay_ThrowsVoidWindowExpiredException()
    {
        var order = CreateOrder(out var product);
        var nextDay = order.CreatedAt.AddDays(1);

        var act = () => order.Void(nextDay, ProductsById(product));

        act.Should().Throw<VoidWindowExpiredException>();
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ThrowsAlreadyVoidedException()
    {
        var order = CreateOrder(out var product);
        order.Void(order.CreatedAt, ProductsById(product));

        var act = () => order.Void(order.CreatedAt, ProductsById(product));

        act.Should().Throw<AlreadyVoidedException>();
    }

    [Fact]
    public void Void_OnSameDay_RestoresEveryLineQuantityBackToProductStock()
    {
        var order = CreateOrder(out var product, quantityOnHand: 20, quantitySold: 3);
        product.QuantityOnHand.Should().Be(17);

        order.Void(order.CreatedAt, ProductsById(product));

        product.QuantityOnHand.Should().Be(20);
    }
}
