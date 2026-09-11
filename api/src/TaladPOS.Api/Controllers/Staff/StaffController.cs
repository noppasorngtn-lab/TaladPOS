using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Auth;
using TaladPOS.Application.StaffManagement;
using AppCreateStaffRequest = TaladPOS.Application.StaffManagement.CreateStaffRequest;
using AppEditStaffRequest = TaladPOS.Application.StaffManagement.EditStaffRequest;

namespace TaladPOS.Api.Controllers.Staff;

// Every endpoint in this controller is Admin-only per contracts/staff.md (FR-006) — a Cashier
// JWT gets 403 on all of them, unlike ProductsController's search endpoint which any staff can call.
[ApiController]
[Route("staff")]
[Authorize(Policy = "Admin")]
public class StaffController(
    ManageStaffUseCases manageStaffUseCases,
    IStaffRepository staffRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StaffSearchResponse>> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await staffRepository.SearchAsync(search, page, pageSize, cancellationToken);
        var items = result.Items.Select(ToSummaryResponse).ToList();
        return Ok(new StaffSearchResponse(items, result.Total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffSummaryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff {id} was not found.");

        return Ok(ToSummaryResponse(staff));
    }

    [HttpPost]
    public async Task<ActionResult<StaffSummaryResponse>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var staff = await manageStaffUseCases.CreateAsync(
            new AppCreateStaffRequest(request.Name, request.Username, request.Password, request.Role),
            User.GetStaffId(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = staff.Id }, ToSummaryResponse(staff));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StaffSummaryResponse>> Update(Guid id, EditStaffRequest request, CancellationToken cancellationToken)
    {
        var staff = await manageStaffUseCases.EditAsync(
            id,
            new AppEditStaffRequest(request.Name, request.Role),
            User.GetStaffId(),
            cancellationToken);

        return Ok(ToSummaryResponse(staff));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await manageStaffUseCases.DeactivateAsync(id, User.GetStaffId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await manageStaffUseCases.ActivateAsync(id, User.GetStaffId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await manageStaffUseCases.ResetPasswordAsync(id, request.NewPassword, User.GetStaffId(), cancellationToken);
        return NoContent();
    }

    private static StaffSummaryResponse ToSummaryResponse(Domain.Entities.Staff staff) =>
        new(staff.Id, staff.Name, staff.Username, staff.Role.ToString(), staff.IsActive);
}
