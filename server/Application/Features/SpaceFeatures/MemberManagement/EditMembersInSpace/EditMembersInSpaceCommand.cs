using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.SpaceFeatures.MemberManagement.EditMembersInSpace;

public record class EditMembersInSpaceCommand(List<Guid> membersId, Guid spaceId, AccessLevel accessLevel) : ICommand<Unit>;