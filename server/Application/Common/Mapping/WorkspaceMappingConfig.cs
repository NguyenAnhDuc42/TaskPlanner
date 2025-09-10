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
            .Map(dest => dest.Color, src => src.Color)
            .Map(dest => dest.Icon, src => src.Icon)
            .Map(dest => dest.Visibility, src => src.Visibility);
        config.NewConfig<ProjectWorkspace, WorkspaceDetail>()
           .Map(dest => dest, src => src.Adapt<WorkspaceSummary>()) // reuse mapping
           .Map(dest => dest.Members, src => src.Members.Adapt<IEnumerable<Member>>())
           .Ignore(dest => dest.CurrentRole); // m
    }
}
