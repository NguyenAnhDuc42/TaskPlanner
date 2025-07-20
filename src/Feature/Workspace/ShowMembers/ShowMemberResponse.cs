namespace src.Feature.Workspace.ShowMembers;


public record Members(List<Member> members);
public record Member(Guid Id, string Name, string Email, string Role);
