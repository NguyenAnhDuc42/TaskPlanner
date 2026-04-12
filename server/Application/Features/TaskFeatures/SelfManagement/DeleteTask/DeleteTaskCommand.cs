using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public record DeleteTaskCommand(Guid TaskId) : ICommandRequest, IAuthorizedWorkspaceRequest;
