using System;
using Application.Contract.DashboardDtos;
using Domain.Entities.Support.Widget;
using Mapster;

namespace Application.Common.Mapping;

public class DashboardMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Dashboard, DashboardListItemDto>()
            .Map(dest => dest.dashboardId, src => src.Id)
            .Map(dest => dest.name, src => src.Name)
            .Map(dest => dest.lastUpdated, src => src.UpdatedAt);
        
    }
}
