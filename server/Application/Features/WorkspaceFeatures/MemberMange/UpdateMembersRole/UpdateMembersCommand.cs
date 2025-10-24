using Application.Common.Interfaces;
using Application.Features.WorkspaceFeatures.MemberMange.DTOs;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberMange.UpdateMembersRole;

public record class UpdateMembersCommand(Guid workspaceId, List<UpdateMemberDto> members) : ICommand<Unit>;
