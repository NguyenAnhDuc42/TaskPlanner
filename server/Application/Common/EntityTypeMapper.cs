using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using System;

namespace Application.Common;

public static class EntityTypeMapper
{
    public static EntityType GetEntityType<TEntity>() where TEntity : Entity =>
        typeof(TEntity).Name switch
        {
            nameof(ProjectWorkspace) => EntityType.ProjectWorkspace,
            nameof(ProjectSpace) => EntityType.ProjectSpace,
            nameof(ProjectFolder) => EntityType.ProjectFolder,
            nameof(ProjectList) => EntityType.ProjectList,
            nameof(ProjectTask) => EntityType.ProjectTask,
            _ => throw new InvalidOperationException($"Unknown entity type: {typeof(TEntity).Name}")
        };
}
