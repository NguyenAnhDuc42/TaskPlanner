using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateSpace;

public record class CreateSpaceCommand(Guid workspaceId,string name,string description,string color,string icon,bool isPrivate) : ICommand<Unit>;