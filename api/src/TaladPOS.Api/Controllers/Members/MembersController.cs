using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Members;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.Members;

[ApiController]
[Route("members")]
[Authorize]
public class MembersController(ManageMemberUseCases manageMemberUseCases, IMemberRepository memberRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MemberResponse>> GetByPhone([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var member = await manageMemberUseCases.FindByPhoneAsync(phone, cancellationToken)
            ?? throw new KeyNotFoundException($"No member found with phone number '{phone}'.");

        return Ok(ToResponse(member));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Member {id} was not found.");

        return Ok(ToResponse(member));
    }

    [HttpPost]
    public async Task<ActionResult<MemberResponse>> SignUp(SignUpMemberRequestDto request, CancellationToken cancellationToken)
    {
        var member = await manageMemberUseCases.SignUpAsync(
            new SignUpMemberRequest(request.Name, request.PhoneNumber), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = member.Id }, ToResponse(member));
    }

    private static MemberResponse ToResponse(Member member) =>
        new(member.Id, member.Name, member.PhoneNumber, member.AccumulatedPurchaseTotal, member.JoinedAt);
}
