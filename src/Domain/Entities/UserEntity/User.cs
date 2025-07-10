using System;
using src.Domain.Entities.SessionEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;
using src.Domain.Valueobject;

namespace src.Domain.Entities.UserEntity;

public class User : Agregate<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;


    public ICollection<Session> Sessions { get; private set; } = new List<Session>();
    public ICollection<UserWorkspace> Workspaces { get; set; } = new List<UserWorkspace>();
    public ICollection<UserSpace> Spaces { get; set; } = new List<UserSpace>();
    public ICollection<UserList> Lists { get; set; } = new List<UserList>();
    public ICollection<UserFolder> Folders { get; set; } = new List<UserFolder>();
    public ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();




    private User() { }
    public User(Guid userId, string name, Email email, string passwordHash) : base(userId)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Create(string name, Email email, string passwordHash)
    {
        return new User(Guid.NewGuid(), name, new Email(email), passwordHash);
    }
    
    public void AddSession(Session session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        Sessions.Add(session);
    }
}
