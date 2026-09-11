using FluentAssertions;
using Moq;
using TaladPOS.Application.Auth;
using TaladPOS.Application.StaffManagement;
using TaladPOS.Domain.Entities;
using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Application.Tests;

public class ManageStaffUseCasesTests
{
    private readonly Mock<IStaffRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly ManageStaffUseCases _useCases;
    private static readonly Guid AdminId = Guid.NewGuid();

    public ManageStaffUseCasesTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _useCases = new ManageStaffUseCases(_repository.Object, _passwordHasher.Object);
    }

    private static Staff NewStaff(StaffRole role = StaffRole.Cashier, bool isActive = true)
    {
        var staff = new Staff("Someone", "someone", "hash", role);
        if (!isActive)
        {
            staff.Deactivate();
        }
        return staff;
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameAlreadyExists_ThrowsArgumentException_AndDoesNotAdd()
    {
        _repository.Setup(r => r.UsernameExistsAsync("dup", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _useCases.CreateAsync(
            new CreateStaffRequest("Name", "dup", "password1", StaffRole.Cashier), AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("bad user")]
    [InlineData("bad$user")]
    public async Task CreateAsync_WhenUsernameHasInvalidCharacters_ThrowsArgumentException(string username)
    {
        var act = () => _useCases.CreateAsync(
            new CreateStaffRequest("Name", username, "password1", StaffRole.Cashier), AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WhenPasswordShorterThan8Characters_ThrowsArgumentException()
    {
        var act = () => _useCases.CreateAsync(
            new CreateStaffRequest("Name", "gooduser", "short1", StaffRole.Cashier), AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsStaffAndAuditLog()
    {
        _repository.Setup(r => r.UsernameExistsAsync("gooduser", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var staff = await _useCases.CreateAsync(
            new CreateStaffRequest("Name", "gooduser", "password1", StaffRole.Cashier), AdminId, CancellationToken.None);

        staff.Username.Should().Be("gooduser");
        _repository.Verify(r => r.AddAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(
            r => r.AddAuditLogAsync(It.Is<StaffAuditLog>(a => a.Action == StaffAuditAction.Created), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenTargetIsSoleActiveAdmin_ThrowsLastActiveAdminException_AndLeavesIsActiveUnchanged()
    {
        var admin = NewStaff(StaffRole.Admin);
        _repository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _repository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _useCases.DeactivateAsync(admin.Id, AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<LastActiveAdminException>();
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WhenTargetIsNotLastActiveAdmin_Succeeds()
    {
        var admin = NewStaff(StaffRole.Admin);
        _repository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _repository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        await _useCases.DeactivateAsync(admin.Id, AdminId, CancellationToken.None);

        admin.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_WhenTargetIsCashier_SucceedsWithoutCheckingActiveAdminCount()
    {
        var cashier = NewStaff(StaffRole.Cashier);
        _repository.Setup(r => r.GetByIdAsync(cashier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cashier);

        await _useCases.DeactivateAsync(cashier.Id, AdminId, CancellationToken.None);

        cashier.IsActive.Should().BeFalse();
        _repository.Verify(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EditAsync_WhenChangingSoleActiveAdminRoleAwayFromAdmin_ThrowsLastActiveAdminException()
    {
        var admin = NewStaff(StaffRole.Admin);
        _repository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _repository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _useCases.EditAsync(admin.Id, new EditStaffRequest("Name", StaffRole.Cashier), AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<LastActiveAdminException>();
        admin.Role.Should().Be(StaffRole.Admin);
    }

    [Fact]
    public async Task EditAsync_WhenRoleChanges_LogsRoleChangedAction()
    {
        var staff = NewStaff(StaffRole.Cashier);
        _repository.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        await _useCases.EditAsync(staff.Id, new EditStaffRequest("Name", StaffRole.Admin), AdminId, CancellationToken.None);

        _repository.Verify(
            r => r.AddAuditLogAsync(It.Is<StaffAuditLog>(a => a.Action == StaffAuditAction.RoleChanged), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EditAsync_WhenOnlyNameChanges_LogsEditedAction()
    {
        var staff = NewStaff(StaffRole.Cashier);
        _repository.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        await _useCases.EditAsync(staff.Id, new EditStaffRequest("New Name", StaffRole.Cashier), AdminId, CancellationToken.None);

        _repository.Verify(
            r => r.AddAuditLogAsync(It.Is<StaffAuditLog>(a => a.Action == StaffAuditAction.Edited), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidPassword_UpdatesPasswordHash_AndLogsPasswordReset()
    {
        var staff = NewStaff();
        _repository.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        await _useCases.ResetPasswordAsync(staff.Id, "newpassword1", AdminId, CancellationToken.None);

        staff.PasswordHash.Should().Be("hash:newpassword1");
        _repository.Verify(
            r => r.AddAuditLogAsync(It.Is<StaffAuditLog>(a => a.Action == StaffAuditAction.PasswordReset), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordShorterThan8Characters_ThrowsArgumentException()
    {
        var staff = NewStaff();
        _repository.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        var act = () => _useCases.ResetPasswordAsync(staff.Id, "short1", AdminId, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
