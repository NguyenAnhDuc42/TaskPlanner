using Application.Common.Interfaces;
using System;
using MediatR;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public record class DeleteWorkspaceCommand(Guid workspaceId) : ICommand<Unit>;
