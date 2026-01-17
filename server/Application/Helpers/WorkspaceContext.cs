using System;
using Microsoft.AspNetCore.Http;

namespace Application.Helpers;

public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public WorkspaceContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid workspaceId
    {
        get
        {
           var id = _httpContextAccessor.HttpContext?.Items["WorkspaceId"] as Guid?;
            
            if (!id.HasValue)
            {
                throw new InvalidOperationException(
                    "Workspace ID not found. Ensure the request includes X-Workspace-Id header or workspaceId query parameter."
                );
            }

            return id.Value;
        }
    }

}
