namespace Domain;

public class Favorite : TenantEntity
{
    public Guid WorkspaceMemberId { get; set; }
    public Guid EntityId { get; set; }
    public EntityLayerType EntityLayerType { get; set; }
    public string OrderKey { get; set; } = null!;

    public Favorite() { }

}