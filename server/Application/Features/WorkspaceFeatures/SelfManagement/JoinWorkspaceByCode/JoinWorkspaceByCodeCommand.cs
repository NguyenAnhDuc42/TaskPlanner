using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;

public record JoinWorkspaceByCodeCommand(string JoinCode) : ICommandRequest<JoinWorkspaceByCodeResult>;

public record JoinWorkspaceByCodeResult(Guid WorkspaceId, string MembershipStatus, bool IsNewMember);

