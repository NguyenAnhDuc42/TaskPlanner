using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Api;

public static class DbContextExtensions
{
    public static async Task<T> ExecuteInTransactionAsync<T>(this DbContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction != null)
        {
            var inlineResult = await action();
            await context.SaveChangesAsync(cancellationToken);
            return inlineResult;
        }

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            try
            {
                var result = await action();
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}

