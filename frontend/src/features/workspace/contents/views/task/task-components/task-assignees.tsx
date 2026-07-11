import { useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { UserPlus, Check, X } from "lucide-react";
import { UserAvatar } from "@/components/user-avatar";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { AssigneeMutations } from "@/mutations/assignee.mutations";

interface TaskAssigneesProps {
  taskId: string;
  // "chips" (default) — compact inline pills, used in the main canvas's attribute strip.
  // "list" — one bordered row per assignee, used in the properties side panel.
  variant?: "chips" | "list";
}

export const TaskAssignees = observer(function TaskAssignees({ taskId, variant = "chips" }: Readonly<TaskAssigneesProps>) {
  const rootStore = useWorkspaceRootStore();
  const allMembers = rootStore.memberStore.all;
  const syncEngine = useSyncEngine();
  const assigneeMutations = useMemo(() => new AssigneeMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  // Assignee is now Bootstrap+Delta covered (like Task/Space/Folder/Status/DocumentBlock) — no
  // bridge fetch needed, plain MobX read.
  const assignees = rootStore.assigneeStore.getByTask(taskId);

  const { canCreateContent } = useWorkspaceRole();
  const [search, setSearch] = useState("");

  const handleToggle = async (memberId: string) => {
    const existing = assignees.find((a) => a.workspaceMemberId === memberId);
    try {
      if (existing) {
        await assigneeMutations.delete(existing.id);
      } else {
        await assigneeMutations.create(taskId, memberId);
      }
    } catch (err) {
      console.error("Failed to toggle assignee", err);
    }
  };

  const filteredMembers = allMembers.filter(
    (m) =>
      m.name.toLowerCase().includes(search.toLowerCase()) ||
      m.email?.toLowerCase().includes(search.toLowerCase()),
  );

  const addTrigger = variant === "list" ? (
    <button
      type="button"
      className="w-full flex items-center gap-2 px-2 py-1 rounded-md border border-dashed border-border/40 text-[10px] font-medium text-muted-foreground hover:text-foreground hover:border-border/70 hover:bg-muted/10 transition-all"
    >
      <UserPlus className="h-2.5 w-2.5" />
      Add assignee
    </button>
  ) : (
    <button
      type="button"
      className="h-5.5 px-1.5 flex items-center gap-1 text-[10px] font-medium text-muted-foreground hover:text-foreground border border-dashed border-border/50 hover:border-border hover:bg-muted/40 rounded-md transition-colors"
    >
      <UserPlus className="h-2.5 w-2.5" />
      Add
    </button>
  );

  if (variant === "list") {
    return (
      <div className="flex flex-col gap-1">
        {assignees.map((assignee) => {
          const member = rootStore.memberStore.getById(assignee.workspaceMemberId);
          if (!member) return null;
          return (
            <div
              key={assignee.workspaceMemberId}
              className="group flex items-center gap-2 px-2 py-1 rounded-md border border-border/40 hover:border-border/70 bg-muted/10 hover:bg-muted/20 transition-all select-none"
            >
              <UserAvatar
                name={member.name}
                avatarUrl={member.avatarUrl}
                className="h-4.5 w-4.5 rounded-sm shrink-0"
                fallbackClassName="text-[8px] rounded-sm"
              />
              <span className="flex-1 min-w-0 truncate text-[11px] font-medium">{member.name}</span>
              {canCreateContent && (
                <button
                  type="button"
                  onClick={() => handleToggle(assignee.workspaceMemberId)}
                  className="text-muted-foreground/40 hover:text-destructive opacity-0 group-hover:opacity-100 transition-all shrink-0"
                >
                  <X className="h-3 w-3" />
                </button>
              )}
            </div>
          );
        })}

        {canCreateContent && (
          <Popover onOpenChange={(open) => { if (!open) setSearch(""); }}>
            <PopoverTrigger asChild>{addTrigger}</PopoverTrigger>
            <PopoverContent
              align="start"
              className="w-48 p-0 gap-0 rounded-md border border-border shadow-md bg-background text-popover-foreground overflow-hidden"
            >
              <div className="px-2 py-1.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground opacity-60">
                Assign Members
              </div>
              <div className="h-px w-full bg-border" />
              <div className="p-0 border-b border-border">
                <Input
                  autoFocus
                  placeholder="Filter members..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="h-8 text-[11px] bg-transparent border-0 focus-visible:ring-0 rounded-none px-2 w-full"
                />
              </div>
              <div className="max-h-[200px] overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
                {filteredMembers.length === 0 ? (
                  <p className="text-[11px] text-muted-foreground/50 text-center py-3">No members found</p>
                ) : (
                  filteredMembers.map((member) => {
                    const isAssigned = assignees.some((a) => a.workspaceMemberId === member.id);
                    return (
                      <button
                        key={member.id}
                        type="button"
                        onClick={() => handleToggle(member.id)}
                        className={cn(
                          "w-full flex items-center gap-2 px-2 py-1.5 text-[11px] font-semibold text-left rounded-none transition-colors cursor-default outline-none select-none",
                          isAssigned
                            ? "bg-muted text-foreground"
                            : "hover:bg-accent hover:text-accent-foreground",
                        )}
                      >
                        <UserAvatar
                          name={member.name}
                          avatarUrl={member.avatarUrl}
                          className="h-5 w-5 rounded-sm shrink-0"
                          fallbackClassName="text-[8px] rounded-sm"
                        />
                        <span className="truncate font-medium flex-1">{member.name}</span>
                        {isAssigned && <Check className="h-3.5 w-3.5 text-primary shrink-0" />}
                      </button>
                    );
                  })
                )}
              </div>
            </PopoverContent>
          </Popover>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-wrap items-center gap-1.5 min-h-7">
      {/* Assigned member chips */}
      {assignees.map((assignee) => {
        const member = rootStore.memberStore.getById(assignee.workspaceMemberId);
        if (!member) return null;
        return (
          <div
            key={assignee.workspaceMemberId}
            className="flex items-center gap-1 bg-transparent hover:bg-muted/50 rounded-md pl-1 pr-2 py-0 h-5 text-[10px] font-semibold select-none transition-colors duration-300"
          >
            <UserAvatar
              name={member.name}
              avatarUrl={member.avatarUrl}
              className="h-3.5 w-3.5 rounded-sm"
              fallbackClassName="text-[7px] leading-none rounded-sm"
            />
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
        <PopoverTrigger asChild>{addTrigger}</PopoverTrigger>

        <PopoverContent
          align="start"
          className="w-48 p-0 gap-0 rounded-md border border-border shadow-md bg-background text-popover-foreground overflow-hidden"
        >
          {/* Header */}
          <div className="px-2 py-1.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground opacity-60">
            Assign Members
          </div>
          <div className="h-px w-full bg-border" />

          {/* Search */}
          <div className="p-0 border-b border-border">
            <Input
              autoFocus
              placeholder="Filter members..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-8 text-[11px] bg-transparent border-0 focus-visible:ring-0 rounded-none px-2 w-full"
            />
          </div>

          {/* Member list */}
          <div className="max-h-[200px] overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            {filteredMembers.length === 0 ? (
              <p className="text-[11px] text-muted-foreground/50 text-center py-3">No members found</p>
            ) : (
              filteredMembers.map((member) => {
                const isAssigned = assignees.some((a) => a.workspaceMemberId === member.id);
                return (
                  <button
                    key={member.id}
                    type="button"
                    onClick={() => handleToggle(member.id)}
                    className={cn(
                      "w-full flex items-center gap-2 px-2 py-1.5 text-[11px] font-semibold text-left rounded-none transition-colors cursor-default outline-none select-none",
                      isAssigned
                        ? "bg-muted text-foreground"
                        : "hover:bg-accent hover:text-accent-foreground",
                    )}
                  >
                    <UserAvatar
                      name={member.name}
                      avatarUrl={member.avatarUrl}
                      className="h-5 w-5 rounded-sm shrink-0"
                      fallbackClassName="text-[8px] rounded-sm"
                    />
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
});
