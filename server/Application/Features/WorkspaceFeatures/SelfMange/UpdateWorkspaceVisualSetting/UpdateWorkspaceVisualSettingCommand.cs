using Application.Contract.WorkspaceContract;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspaceVisualSetting;

public record class UpdateWorkspaceVisualSettingCommand(Guid workspaceId,string? icon,string color) : IRequest<WorkspaceDetail>;