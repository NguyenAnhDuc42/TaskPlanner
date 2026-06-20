import { useState } from "react";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useSelector } from "react-redux";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { UserPlus, Check, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import type { RootState } from "@/store";
import { memberSelectors, assigneeSelectors, entityAccessSelectors } from "@/store/entityStore";
import { cn } from "@/lib/utils";

import { useGetTaskAssigneesQuery, useUpdateTaskAssigneesMutation } from "../task-api";
import { useGetEntityAccessQuery } from "../../space/space-api";

interface TaskAssigneesProps {
  taskId: string;
  spaceId?: string | null;
}

export function TaskAssignees({ taskId, spaceId }: Readonly<TaskAssigneesProps>) {
  const members = useSelector(memberSelectors.selectEntities);
  const allMembers = useSelector(memberSelectors.selectAll);

  useGetTaskAssigneesQuery(taskId, { skip: !taskId });
  const [updateTaskAssignees] = useUpdateTaskAssigneesMutation();

  const allAssignees = useSelector(assigneeSelectors.selectAll);
  const assignees = allAssignees.filter((a) => a.taskId === taskId);

  const space = useSelector((state: RootState) => state.spaces.entities[spaceId || ""]);
  useGetEntityAccessQuery(spaceId || "", { skip: !spaceId || !space?.isPrivate });

  const entityAccessList = useSelector(entityAccessSelectors.selectAll).filter(
    (ea) => ea.spaceId === spaceId,
  );
  const spaceAccessList = entityAccessList.filter((ea) => ea.haveAccess);

  const { canCreateContent } = useWorkspaceRole();
  const [search, setSearch] = useState("");

  const handleToggle = (memberId: string) => {
    const existing = assignees.find((a) => a.workspaceMemberId === memberId);
    updateTaskAssignees({
      taskId,
      changes: [{ id: existing?.id, memberId, isDelete: !!existing }],
    });
  };

  const allowedMembers = space?.isPrivate
    ? allMembers.filter((m) => spaceAccessList.some((ea) => ea.workspaceMemberId === m.id))
    : allMembers;

  const filteredMembers = allowedMembers.filter(
    (m) =>
      m.name.toLowerCase().includes(search.toLowerCase()) ||
      m.email?.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <div className="flex flex-wrap items-center gap-1.5 min-h-7">
      {/* Assigned member chips */}
      {assignees.map((assignee) => {
        const member = members[assignee.workspaceMemberId];
        if (!member) return null;
        const initials = member.name
          .split(" ")
          .map((n: string) => n[0])
          .join("")
          .slice(0, 2)
          .toUpperCase();
        return (
          <div
            key={assignee.workspaceMemberId}
            className="flex items-center gap-1 bg-transparent hover:bg-muted/50 rounded-sm pl-1 pr-2 py-0 h-5 text-[10px] font-semibold select-none transition-colors duration-300"
          >
            <Avatar className="h-3.5 w-3.5 rounded-sm">
              {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
              <AvatarFallback className="text-[7px] bg-primary/20 text-primary leading-none flex items-center justify-center rounded-sm">
                {initials}
              </AvatarFallback>
            </Avatar>
            <span className="max-w-20 truncate font-medium">{member.name}</span>
            {canCreateContent && (
              <button
                type="button"
                onClick={() => handleToggle(assignee.workspaceMemberId)}
                className="text-muted-foreground hover:text-foreground ml-0.5 transition-colors"
              >
                <X className="h-2.5 w-2.5" />
              </button>
            )}
          </div>
        );
      })}

      {/* Add assignee — Members+ only */}
      {canCreateContent && <Popover onOpenChange={(open) => { if (!open) setSearch(""); }}>
        <PopoverTrigger asChild>
          <button
            type="button"
            className="h-5.5 px-1.5 flex items-center gap-1 text-[10px] font-medium text-muted-foreground hover:text-foreground border border-dashed border-border/50 hover:border-border hover:bg-muted/40 rounded-sm transition-colors"
          >
            <UserPlus className="h-2.5 w-2.5" />
            Add
          </button>
        </PopoverTrigger>

        <PopoverContent
          align="start"
          className="w-48 p-0 rounded-md border border-border shadow-md bg-popover text-popover-foreground overflow-hidden"
        >
          {/* Header — matches DropdownMenuLabel */}
          <div className="px-2 py-1.5 text-xs font-semibold border-b border-border/40">
            Assign Members
          </div>

          {/* Search */}
          <div className="p-1">
            <Input
              autoFocus
              placeholder="Filter members..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-7 text-[11px] bg-muted/30 border-0 focus-visible:ring-0 rounded-sm px-2"
            />
          </div>

          {/* Member list — matches DropdownMenuItem style */}
          <div className="max-h-[200px] overflow-y-auto p-1 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            {filteredMembers.length === 0 ? (
              <p className="text-[11px] text-muted-foreground/50 text-center py-3">No members found</p>
            ) : (
              filteredMembers.map((member) => {
                const isAssigned = assignees.some((a) => a.workspaceMemberId === member.id);
                const initials = member.name
                  .split(" ")
                  .map((n: string) => n[0])
                  .join("")
                  .slice(0, 2)
                  .toUpperCase();
                return (
                  <button
                    key={member.id}
                    type="button"
                    onClick={() => handleToggle(member.id)}
                    className={cn(
                      "w-full flex items-center gap-2 px-2 py-1.5 text-[11px] text-left rounded-sm transition-colors",
                      isAssigned
                        ? "bg-muted text-foreground shadow-sm"
                        : "hover:bg-accent hover:text-accent-foreground",
                    )}
                  >
                    <Avatar className="h-5 w-5 rounded-sm shrink-0">
                      {member.avatarUrl && <AvatarImage src={member.avatarUrl} alt={member.name} />}
                      <AvatarFallback className="text-[8px] bg-primary/10 text-primary rounded-sm">
                        {initials}
                      </AvatarFallback>
                    </Avatar>
                    <span className="truncate font-medium flex-1">{member.name}</span>
                    {isAssigned && <Check className="h-3.5 w-3.5 text-primary shrink-0" />}
                  </button>
                );
              })
            )}
          </div>
        </PopoverContent>
      </Popover>}
    </div>
  );
}
