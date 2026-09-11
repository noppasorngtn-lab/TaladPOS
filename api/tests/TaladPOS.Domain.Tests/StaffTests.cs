using FluentAssertions;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Domain.Tests;

public class StaffTests
{
    private static Staff CreateStaff() =>
        new("Cashier One", "cashier1", "hash", StaffRole.Cashier);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WhenNameIsBlank_ThrowsArgumentException(string name)
    {
        var staff = CreateStaff();

        var act = () => staff.Rename(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var staff = CreateStaff();

        staff.Rename("Cashier Renamed");

        staff.Name.Should().Be("Cashier Renamed");
    }

    [Fact]
    public void ChangeRole_UpdatesRole()
    {
        var staff = CreateStaff();

        staff.ChangeRole(StaffRole.Admin);

        staff.Role.Should().Be(StaffRole.Admin);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        var staff = CreateStaff();
        staff.Deactivate();

        staff.Activate();

        staff.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var staff = CreateStaff();

        staff.Deactivate();

        staff.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPasswordHash_WhenHashIsBlank_ThrowsArgumentException(string hash)
    {
        var staff = CreateStaff();

        var act = () => staff.SetPasswordHash(hash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_WithValidHash_UpdatesPasswordHash()
    {
        var staff = CreateStaff();

        staff.SetPasswordHash("new-hash");

        staff.PasswordHash.Should().Be("new-hash");
    }
}
