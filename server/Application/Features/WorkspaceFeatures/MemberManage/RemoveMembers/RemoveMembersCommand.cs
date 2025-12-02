using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public record class RemoveMembersCommand(Guid workspaceId, List<Guid> memberIds) : ICommand<Unit>;