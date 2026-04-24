using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record JoinWorkspaceByCodeCommand(string JoinCode) : ICommandRequest<JoinWorkspaceByCodeResult>;

public record JoinWorkspaceByCodeResult(Guid WorkspaceId, string MembershipStatus, bool IsNewMember);

