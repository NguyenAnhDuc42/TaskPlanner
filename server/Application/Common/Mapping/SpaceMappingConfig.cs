using System;
using Application.Contract.SpaceContract;
using Domain.Entities.ProjectEntities;
using Mapster;

namespace Application.Common.Mapping;

public class SpaceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
         config.NewConfig<ProjectSpace, SpaceSummary>()
            .Map(dest => dest.spaceId, src => src.Id)
            .Map(dest => dest.workspaceId, src => src.ProjectWorkspaceId);
    }
}
