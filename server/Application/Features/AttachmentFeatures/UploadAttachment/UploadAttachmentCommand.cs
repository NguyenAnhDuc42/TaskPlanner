using Microsoft.AspNetCore.Http;

namespace Application;

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


