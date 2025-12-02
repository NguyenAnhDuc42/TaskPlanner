// This mapping config is no longer needed as we're manually mapping DTOs
// Keeping file for potential future use

using Application.Contract.WorkspaceContract;
using Domain.Entities.ProjectEntities;
using Mapster;

namespace Application.Common.Mapping;

public class WorkspaceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Mappings removed - using manual DTO construction instead
    }
}
