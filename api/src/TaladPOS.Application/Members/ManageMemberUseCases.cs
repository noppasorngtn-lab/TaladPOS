using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Members;

public record SignUpMemberRequest(string Name, string PhoneNumber);

/// <summary>Member signup and phone lookup for the Sales screen membership flow (FR-015–FR-017).</summary>
public class ManageMemberUseCases(IMemberRepository memberRepository)
{
    public async Task<Member> SignUpAsync(SignUpMemberRequest request, CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();
        var isTaken = await memberRepository.PhoneNumberExistsAsync(phoneNumber, cancellationToken);
        Member.EnsurePhoneNumberIsAvailable(phoneNumber, isTaken);

        var member = new Member(request.Name.Trim(), phoneNumber);

        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);
        return member;
    }

    public Task<Member?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        memberRepository.GetByPhoneNumberAsync(phoneNumber.Trim(), cancellationToken);
}
