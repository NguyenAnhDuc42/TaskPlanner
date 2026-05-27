using System;
using System.Collections.Generic;

namespace Application;

public class EntityBatchUpdate
{
    public List<SpaceRecord>? Spaces { get; set; }
    public List<FolderRecord>? Folders { get; set; }
    public List<TaskRecord>? Tasks { get; set; }
    public List<MemberRecord>? Members { get; set; }
}

public class EntityBatchDelete
{
    public List<Guid>? SpaceIds { get; set; }
    public List<Guid>? FolderIds { get; set; }
    public List<Guid>? TaskIds { get; set; }
    public List<Guid>? MemberIds { get; set; }
}
