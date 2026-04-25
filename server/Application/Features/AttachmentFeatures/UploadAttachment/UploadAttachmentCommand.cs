using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Features.AttachmentFeatures;

public record UploadAttachmentCommand 
(   
    AttachmentType Type,
    Guid ParentEntityId,
    EntityType EntityType,
    IFormFile? File,
    string? Url, 
    string? Title,
    string? Description,
    string? ImageUrl,
    string? Provider 
) : ICommandRequest;
