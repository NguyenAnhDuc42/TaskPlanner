using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures;

public record UpdateViewCommand(Guid Id, string Name) : ICommandRequest, IAuthorizedWorkspaceRequest;
