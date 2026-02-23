using Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.ViewFeatures.Logic;

public static class DocumentSql
{
    public static async Task<object> Execute(IServiceProvider sp, Guid layerId)
    {
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

        const string Sql = @"
            SELECT * 
            FROM documents 
            WHERE layer_id = @layerId AND is_archived = false";

        var result = await unitOfWork.QueryAsync<dynamic>(Sql, new { layerId });
        return result;
    }
}
