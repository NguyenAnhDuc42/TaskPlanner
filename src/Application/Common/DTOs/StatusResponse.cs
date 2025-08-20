using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Application.Common.DTOs;


public record WorkloadSummaryResponse(int TotalTaskCount, List<StatusWorkloadSummary> StatusBreakdown);


public record StatusWorkloadSummary(Guid StatusId, string Name, string Color, int TaskCount, double Percentage);