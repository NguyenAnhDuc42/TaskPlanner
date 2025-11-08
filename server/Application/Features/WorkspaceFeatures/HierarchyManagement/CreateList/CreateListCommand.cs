using System;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateList;

public record CreateListCommand(Guid spaceId,Guid? folderId,string name,string color,string icon,bool isPrivate,DateTimeOffset? startDate,DateTimeOffset? dueDate) : ICommand<Unit>;

