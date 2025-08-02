using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Contract;


public record WorkloadSummaryResponse(int TotalTaskCount, List<StatusWorkloadSummary> StatusBreakdown);


public record StatusWorkloadSummary(Guid StatusId, string Name, string Color, int TaskCount, double Percentage);


public record class StatusDto(Guid StatusId, string Name, string Color, StatusType Type);
