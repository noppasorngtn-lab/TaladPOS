namespace TaladPOS.Api.Controllers.Members;

public record SignUpMemberRequestDto(string Name, string PhoneNumber);

public record MemberResponse(Guid Id, string Name, string PhoneNumber, decimal AccumulatedPurchaseTotal, DateTimeOffset JoinedAt);
