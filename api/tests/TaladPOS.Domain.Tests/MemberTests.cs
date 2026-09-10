using FluentAssertions;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Domain.Tests;

public class MemberTests
{
    [Fact]
    public void EnsurePhoneNumberIsAvailable_WhenPhoneNumberIsAlreadyTakenByAnotherMember_ThrowsArgumentException()
    {
        var act = () => Member.EnsurePhoneNumberIsAvailable("0812345678", isPhoneNumberTakenByAnotherMember: true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsurePhoneNumberIsAvailable_WhenPhoneNumberIsNotTaken_DoesNotThrow()
    {
        var act = () => Member.EnsurePhoneNumberIsAvailable("0812345678", isPhoneNumberTakenByAnotherMember: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void Credit_IncreasesAccumulatedPurchaseTotalByACompletedOrdersNetTotal()
    {
        var member = new Member("Somchai", "0812345678");
        var product = new Product("Mango", price: 25m, quantityOnHand: 10);
        var order = SalesOrder.Checkout(Guid.NewGuid(), member.Id, [(product, 3)]);

        member.Credit(order.NetTotal);

        member.AccumulatedPurchaseTotal.Should().Be(order.NetTotal);
    }

    [Fact]
    public void ReverseCredit_DecreasesAccumulatedPurchaseTotalBackToZeroWhenThatOrderIsVoided()
    {
        var member = new Member("Somchai", "0812345678");
        var product = new Product("Mango", price: 25m, quantityOnHand: 10);
        var order = SalesOrder.Checkout(Guid.NewGuid(), member.Id, [(product, 3)]);
        member.Credit(order.NetTotal);

        order.Void(order.CreatedAt, new Dictionary<Guid, Product> { [product.Id] = product });
        member.ReverseCredit(order.NetTotal);

        member.AccumulatedPurchaseTotal.Should().Be(0m);
    }
}
