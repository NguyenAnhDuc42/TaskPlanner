using Domain.Common;
using Domain.Entities;
using Domain.Enums;
namespace Infrastructure.Services;

public static class EntityTypeMapper
{
    public static EntityType GetEntityType<TEntity>() where TEntity : Entity =>
        typeof(TEntity).Name switch
        {
            nameof(ProjectWorkspace) => EntityType.ProjectWorkspace,
            nameof(ProjectSpace) => EntityType.ProjectSpace,
            nameof(ProjectFolder) => EntityType.ProjectFolder,
            nameof(ProjectTask) => EntityType.ProjectTask,
            _ => throw new InvalidOperationException($"Unknown entity type: {typeof(TEntity).Name}")
        };
}


