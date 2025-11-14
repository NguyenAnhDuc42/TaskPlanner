namespace Application.Contract.DashboardDtos;

public record class DashboardListDto(IEnumerable<DashboardListItemDto> items);
public record class DashboardListItemDto(Guid dashboardId,string name,DateTimeOffset lastUpdated);
