using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures;

public record DeleteViewCommand(Guid Id) : ICommandRequest, IAuthorizedWorkspaceRequest;
