using Application.Common.Interfaces;

namespace Application.Features.AttachmentFeatures.DeleteAttachment;

public record DeleteAttachmentCommand(Guid AttachmentId) : ICommandRequest;
