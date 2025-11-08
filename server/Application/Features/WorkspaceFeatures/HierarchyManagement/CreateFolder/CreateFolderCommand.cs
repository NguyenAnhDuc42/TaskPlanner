using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateFolder;

public record class CreateFolderCommand(Guid spaceId,string name,string color,string icon,bool isPrivate) : ICommand<Unit>;