using System;
using Application.Contract.UserContract;
using Application.Contract.WorkspaceContract;
using Domain.Entities.ProjectEntities;
using Mapster;

namespace Application.Common.Mapping;

public class WorkspaceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProjectWorkspace, WorkspaceSummary>()
            .Map(dest => dest.WorkspaceId, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Color, src => src.Customization.Color)
            .Map(dest => dest.Icon, src => src.Customization.Icon)
            .Map(dest => dest.Variant, src => src.Variant.ToString());
        config.NewConfig<ProjectWorkspace, WorkspaceDetail>()
           .Map(dest => dest, src => src.Adapt<WorkspaceSummary>());
    }
}
