import { useState } from "react";
import { useSelector } from "react-redux";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { UserPlus, Check, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import type { RootState } from "@/store";
import { memberSelectors, assigneeSelectors, entityAccessSelectors } from "@/store/entityStore";

import { useGetTaskAssigneesQuery, useUpdateTaskAssigneesMutation } from "../task-api";
import { useGetEntityAccessQuery } from "../../space/space-api";

interface TaskAssigneesProps {
  taskId: string;
  spaceId?: string | null;
}

export function TaskAssignees({ taskId, spaceId }: Readonly<TaskAssigneesProps>) {
  const members = useSelector(memberSelectors.selectEntities);
  const allMembers = useSelector(memberSelectors.selectAll);

  useGetTaskAssigneesQuery(taskId, {
    skip: !taskId,
  });

  const [updateTaskAssignees] = useUpdateTaskAssigneesMutation();

  const allAssignees = useSelector(assigneeSelectors.selectAll);
  const assignees = allAssignees.filter(a => a.taskId === taskId);

  const space = useSelector((state: RootState) => state.spaces.entities[spaceId || ""]);

  useGetEntityAccessQuery(spaceId || "", {
    skip: !spaceId || !space?.isPrivate,
  });

  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(ea => ea.spaceId === spaceId);
  const spaceAccessList = entityAccessList.filter(ea => ea.haveAccess);

  const [assigneeSearch, setAssigneeSearch] = useState("");

  const handleToggleAssignee = (memberId: string) => {
    const existing = assignees.find((a) => a.workspaceMemberId === memberId);
    const isAssigned = !!existing;
    updateTaskAssignees({
      taskId,
      changes: [{ id: existing?.id, memberId, isDelete: isAssigned }]
    });
  };

  const allowedMembers = space?.isPrivate
    ? allMembers.filter(m => spaceAccessList.some(ea => ea.workspaceMemberId === m.id))
    : allMembers;

  const filteredMembers = allowedMembers.filter((m) =>
    m.name.toLowerCase().includes(assigneeSearch.toLowerCase()) ||
    m.email?.toLowerCase().includes(assigneeSearch.toLowerCase())
  );

  return (
    <div className="flex flex-wrap items-center gap-1.5 min-h-7">
      {assignees.map((assignee) => {
        const member = members[assignee.workspaceMemberId];
        if (!member) return null;
        const initials = member.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
        return (
          <div key={assignee.workspaceMemberId} className="flex items-center gap-1 bg-muted/30 border border-border/50 rounded-md pl-1 pr-1.5 py-0.5 text-[10px] h-5.5 select-none leading-none">
            <Avatar className="h-3.5 w-3.5 rounded-sm">
              {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
              <AvatarFallback className="text-[7px] bg-primary/20 text-primary leading-none flex items-center justify-center rounded-sm">{initials}</AvatarFallback>
            </Avatar>
            <span className="max-w-[80px] truncate font-medium">{member.name}</span>
            <button
              type="button"
              onClick={() => handleToggleAssignee(assignee.workspaceMemberId)}
              className="text-muted-foreground hover:text-foreground ml-0.5"
            >
              <X className="h-2.5 w-2.5" />
            </button>
          </div>
        );
      })}

      <Popover>
        <PopoverTrigger asChild>
          <Button variant="ghost" size="sm" className="h-5.5 px-1.5 text-[10px] text-muted-foreground hover:text-foreground border border-dashed border-border/50 hover:bg-muted/50 rounded-md">
            <UserPlus className="h-2.5 w-2.5 mr-1" /> Add Assignee
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-60 p-2 bg-popover border border-border shadow-md rounded-md" align="start">
          <div className="px-2 py-1.5 border-b border-border/10 mb-2">
            <span className="text-[8px] font-black uppercase tracking-wider text-muted-foreground/50">Assign Members</span>
          </div>
          <Input
            placeholder="Filter members..."
            value={assigneeSearch}
            onChange={(e) => setAssigneeSearch(e.target.value)}
            className="h-7 text-xs mb-1.5 bg-transparent border-none focus-visible:ring-0 px-2"
            autoFocus
          />
          <div className="max-h-[200px] overflow-y-auto flex flex-col pr-1 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            {filteredMembers.map((member) => {
              const memberId = member.id;
              const isAssigned = assignees.some((a) => a.workspaceMemberId === memberId);
              const initials = member.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2).toUpperCase();
              return (
                <button
                  key={memberId}
                  type="button"
                  onClick={() => handleToggleAssignee(memberId)}
                  className="w-full flex items-center justify-between px-2 py-1.5 text-xs text-left rounded-md hover:bg-muted/60 transition-colors group"
                >
                  <div className="flex items-center gap-2.5">
                    <Avatar className="h-5 w-5">
                      {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
                      <AvatarFallback className="text-[8px] bg-primary/10 text-primary">{initials}</AvatarFallback>
                    </Avatar>
                    <span className="truncate font-medium text-foreground/80 group-hover:text-foreground">{member.name}</span>
                  </div>
                  {isAssigned && <Check className="h-3.5 w-3.5 text-primary" />}
                </button>
              );
            })}
            {filteredMembers.length === 0 && (
              <p className="text-[10px] text-muted-foreground/50 text-center py-2">No members found</p>
            )}
          </div>
        </PopoverContent>
      </Popover>
    </div>
  );
}
