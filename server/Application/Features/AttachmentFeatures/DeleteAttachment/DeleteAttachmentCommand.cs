using Application.Common.Interfaces;

namespace Application.Features.AttachmentFeatures;

public record DeleteAttachmentCommand(Guid AttachmentId) : ICommandRequest;
