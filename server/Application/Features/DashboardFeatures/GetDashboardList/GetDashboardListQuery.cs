using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Contract.DashboardDtos;
using Domain.Enums.RelationShip;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.GetDashboardList;

public record class GetDashboardListQuery(CursorPaginationRequest pagination,Guid layerId,EntityLayerType layerType,DashboardFilter filter) : IQuery<PagedResult<DashboardListItemDto>>;