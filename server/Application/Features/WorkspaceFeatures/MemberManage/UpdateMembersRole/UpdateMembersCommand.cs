using Application.Common.Interfaces;
using Application.Features.WorkspaceFeatures.MemberManage.DTOs;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public record class UpdateMembersCommand(Guid workspaceId, List<UpdateMemberDto> members) : ICommand<Unit>;
