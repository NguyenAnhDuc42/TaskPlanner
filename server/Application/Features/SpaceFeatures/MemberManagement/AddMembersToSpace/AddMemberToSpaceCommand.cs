using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.SpaceFeatures.MemberManagement.AddMembersToSpace;

public record class AddMemberToSpaceCommand(List<Guid> membersId,AccessLevel accessLevel, Guid spaceId) : ICommand<Unit>;