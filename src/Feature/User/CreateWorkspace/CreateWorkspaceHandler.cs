using System;
using MediatR;
using src.Helper.Results;

namespace src.Feature.User.CreateWorkspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceRequest,Result<string, ErrorResponse>>
{
    public Task<Result<string, ErrorResponse>> Handle(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        // Implementation of the workspace creation logic goes here.
        // This is a placeholder for the actual logic that would create a workspace
        // and return the result.

        throw new NotImplementedException("Workspace creation logic is not implemented yet.");
    }
}
{

}
