using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.AttachmentFeatures.LinkAttachment;

public record LinkAttachmentCommand(
    Guid AttachmentId, 
    Guid ParentEntityId, 
    EntityType ParentEntityType
) : ICommandRequest;
