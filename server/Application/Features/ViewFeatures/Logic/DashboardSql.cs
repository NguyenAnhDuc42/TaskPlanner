using Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.ViewFeatures.Logic;

public static class DashboardSql
{
    public static async Task<object> Execute(IServiceProvider sp, Guid layerId)
    {
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

        // For now, we fetch the first dashboard found for this layer.
        // In a more advanced version, we might have multiple dashboards per layer.
        const string Sql = @"
            SELECT d.*, w.*
            FROM dashboards d
            LEFT JOIN widgets w ON d.id = w.dashboard_id
            WHERE d.layer_id = @layerId AND d.is_archived = false";

        var result = await unitOfWork.QueryAsync<dynamic>(Sql, new { layerId });
        return result;
    }
}
