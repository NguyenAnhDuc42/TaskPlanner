using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.SpaceFeatures.MemberManagement.RemoveMembersFromSpace;

public record class RemoveMembersFromSpaceCommand(List<Guid> membersId, Guid spaceId) : ICommand<Unit>;