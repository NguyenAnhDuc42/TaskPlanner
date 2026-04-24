using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures;

public record DeleteTaskCommand(Guid TaskId) : ICommandRequest, IAuthorizedWorkspaceRequest;
