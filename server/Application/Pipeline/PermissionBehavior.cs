using System;
using System.Reflection;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using server.Application.Interfaces;

namespace Application.Pipeline;

public class PermissionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    public PermissionBehavior(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = typeof(TRequest).GetCustomAttribute<RequirePermissionAttribute>();
        if (attribute == null) return await next();

        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();
        var entityId = ExtractEntityId(request, attribute.EntityType);
        
        using var scope = _serviceProvider.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var entityExists = await CheckEntityExistsAsync(unitOfWork, attribute.EntityType, entityId, cancellationToken);
        if (!entityExists) throw new KeyNotFoundException($"Entity of type {attribute.EntityType} with ID {entityId} was not found.");

        var hasPermission = await permissionService.HasPermissionAsync(
            userId,
            entityId,
            attribute.EntityType,
            attribute.RequiredPermission,
            cancellationToken
        );

        if (!hasPermission) throw new UnauthorizedAccessException($"Permission denied.");

        return await next();
    }

    private Guid ExtractEntityId(TRequest request, EntityType entityType)
    {
        Guid? entityId = null;

        if (entityType == EntityType.ProjectSpace)
        {
            entityId = GetProperty<Guid>(request, "Id");
            if (entityId == null)
            {
                entityId = GetProperty<Guid>(request, "EntityId");
            }
        }
        else if (entityType == EntityType.ProjectFolder)
        {
            entityId = GetProperty<Guid>(request, "FolderId");
            if (entityId == null)
            {
                entityId = GetProperty<Guid>(request, "Id");
            }
        }
        else if (entityType == EntityType.ProjectList)
        {
            entityId = GetProperty<Guid>(request, "ListId");
            if (entityId == null)
            {
                entityId = GetProperty<Guid>(request, "Id");
            }
        }
        else
        {
            throw new NotSupportedException($"Entity type {entityType} is not supported.");
        }

        if (entityId == null || entityId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Could not find a valid entity ID on request '{typeof(TRequest).Name}' for entity type '{entityType}'. " +
                "Expected a property like 'Id', 'EntityId', 'SpaceId', 'FolderId', or 'ListId'.");
        }

        return entityId.Value;
    }

    private static T? GetProperty<T>(TRequest request, string propertyName)
    {
        return (T?)typeof(TRequest).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(request);
    }

    private async Task<bool> CheckEntityExistsAsync(IUnitOfWork unitOfWork, EntityType entityType, Guid entityId, CancellationToken ct)
    {
        return entityType switch
        {
            EntityType.ProjectSpace => await _unitOfWork.Set<ProjectSpace>().AnyAsync(s => s.Id == entityId, ct),
            EntityType.ProjectFolder => await _unitOfWork.Set<ProjectFolder>().AnyAsync(f => f.Id == entityId, ct),
            EntityType.ProjectList => await _unitOfWork.Set<ProjectList>().AnyAsync(l => l.Id == entityId, ct),
            _ => false
        };
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public EntityType EntityType { get; }
    public Permission RequiredPermission { get; }

    public RequirePermissionAttribute(EntityType entityType, Permission requiredPermission)
    {
        EntityType = entityType;
        RequiredPermission = requiredPermission;
    }
}