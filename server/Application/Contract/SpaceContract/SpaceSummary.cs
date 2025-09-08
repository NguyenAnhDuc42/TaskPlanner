namespace Application.Contract.SpaceContract;

public record class SpaceSummary(Guid workspaceId,Guid spaceId,string name,string icon,string color,long orderKey);
