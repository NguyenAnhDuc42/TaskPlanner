using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.WorkspaceFeatures.MemberManage.DTOs;

public record class UpdateMemberDto(Guid? userId, string? email, Role? role, MembershipStatus? status, string? joinMethod);