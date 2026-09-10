using FluentAssertions;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Domain.Tests;

public class ProductTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPriceIsZeroOrNegative_ThrowsArgumentOutOfRangeException(decimal price)
    {
        var act = () => new Product("Mango", price, quantityOnHand: 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WhenQuantityOnHandIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new Product("Mango", price: 10m, quantityOnHand: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Edit_WhenPriceIsZeroOrNegative_ThrowsArgumentOutOfRangeException(decimal price)
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 10);

        var act = () => product.Edit("Mango", price, quantityOnHand: 10, imageUrl: null, barcode: null, lowStockThreshold: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Edit_WhenQuantityOnHandIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 10);

        var act = () => product.Edit("Mango", price: 10m, quantityOnHand: -1, imageUrl: null, barcode: null, lowStockThreshold: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Edit_WithValidValues_UpdatesFieldsInPlace()
    {
        var product = new Product("Mango", price: 10m, quantityOnHand: 10);

        product.Edit("Mango (Ripe)", price: 12m, quantityOnHand: 15, imageUrl: "/img.png", barcode: "123", lowStockThreshold: 5);

        product.Name.Should().Be("Mango (Ripe)");
        product.Price.Should().Be(12m);
        product.QuantityOnHand.Should().Be(15);
        product.ImageUrl.Should().Be("/img.png");
        product.Barcode.Should().Be("123");
        product.LowStockThreshold.Should().Be(5);
    }

    [Fact]
    public void EnsureBarcodeIsAvailable_WhenBarcodeIsAlreadyTakenByAnotherProduct_ThrowsArgumentException()
    {
        var act = () => Product.EnsureBarcodeIsAvailable("12345", isBarcodeTakenByAnotherProduct: true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureBarcodeIsAvailable_WhenBarcodeIsNull_NeverThrowsEvenIfFlagIsTrue()
    {
        var act = () => Product.EnsureBarcodeIsAvailable(null, isBarcodeTakenByAnotherProduct: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureBarcodeIsAvailable_WhenBarcodeIsNotTaken_DoesNotThrow()
    {
        var act = () => Product.EnsureBarcodeIsAvailable("12345", isBarcodeTakenByAnotherProduct: false);

        act.Should().NotThrow();
    }
}
