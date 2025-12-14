using Application.Contract.UserContract;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public record class GetMembersQuery(Guid WorkspaceId) : IRequest<List<MemberDto>>;
