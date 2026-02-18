import { useState, useMemo } from "react";
import { useMembers } from "../../members/members-api";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Search, UserPlus, UserX } from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import type { MemberSummary } from "../../members/members-type";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  ASSIGNABLE_ACCESS_LEVELS,
  type AssignableAccessLevel,
} from "@/types/access-level";

interface Props {
  workspaceId: string;
  selectedIds: string[];
  selectedAccessLevels: Record<string, AssignableAccessLevel>;
  creatorWorkspaceMemberId?: string;
  onToggle: (id: string) => void;
  onAccessLevelChange: (id: string, level: AssignableAccessLevel) => void;
}

export function MemberSelector({
  workspaceId,
  selectedIds,
  selectedAccessLevels,
  creatorWorkspaceMemberId,
  onToggle,
  onAccessLevelChange,
}: Props) {
  const [search, setSearch] = useState("");
  const { data: membersPack, isLoading } = useMembers(workspaceId, {
    name: search || undefined,
  });

  const allMembers = useMemo(() => {
    return membersPack?.pages.flatMap((page: any) => page.items) || [];
  }, [membersPack]);

  const sortedMembers = useMemo(() => {
    return [...allMembers].sort((a: MemberSummary, b: MemberSummary) => {
      const aSelected = selectedIds.some(
        (id) => id.toLowerCase() === a.workspaceMemberId.toLowerCase(),
      );
      const bSelected = selectedIds.some(
        (id) => id.toLowerCase() === b.workspaceMemberId.toLowerCase(),
      );

      if (aSelected !== bSelected) return aSelected ? -1 : 1;

      const aIsCreator =
        creatorWorkspaceMemberId?.toLowerCase() ===
        a.workspaceMemberId.toLowerCase();
      const bIsCreator =
        creatorWorkspaceMemberId?.toLowerCase() ===
        b.workspaceMemberId.toLowerCase();
      if (aIsCreator !== bIsCreator) return aIsCreator ? -1 : 1;

      return (a.name || "").localeCompare(b.name || "");
    });
  }, [allMembers, selectedIds, creatorWorkspaceMemberId]);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div className="text-xs font-medium text-muted-foreground">Members</div>
        <Badge variant="outline" className="text-[10px]">
          {selectedIds.length} selected
        </Badge>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search workspace members..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9 h-9"
        />
      </div>

      <ScrollArea className="h-[260px] border rounded-md p-2">
        {isLoading ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            Loading members...
          </div>
        ) : allMembers.length === 0 ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            No members found.
          </div>
        ) : (
          <div className="space-y-2">
            {sortedMembers.map((member: MemberSummary) => {
              const memberId = member.workspaceMemberId.toLowerCase();
              const selected = selectedIds.includes(memberId);
              const isCreator =
                creatorWorkspaceMemberId?.toLowerCase() === memberId;

              return (
                <div
                  key={memberId}
                  className={cn(
                    "w-full flex items-center gap-2 px-2 py-2 rounded-md border text-sm transition-colors",
                    selected
                      ? "bg-primary/10 border-primary/40 text-foreground"
                      : "bg-card border-border hover:bg-muted",
                  )}
                >
                  <div className="flex-1 min-w-0 flex items-center gap-3">
                    <div className="flex-1 text-left min-w-0">
                      <div className="font-medium truncate">{member.name}</div>
                      <div className="text-[10px] text-muted-foreground truncate mt-0.5">
                        {member.email}
                      </div>
                      {isCreator ? (
                        <div className="text-[10px] text-primary mt-0.5">
                          Creator
                        </div>
                      ) : null}
                    </div>
                  </div>
                  <Select
                    value={
                      selectedAccessLevels[memberId.toLowerCase()] ?? "Editor"
                    }
                    onValueChange={(value) =>
                      onAccessLevelChange(
                        memberId.toLowerCase(),
                        value as AssignableAccessLevel,
                      )
                    }
                    disabled={!selected || isCreator}
                  >
                    <SelectTrigger
                      size="sm"
                      className="w-[92px] shrink-0"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <SelectValue placeholder="Access" />
                    </SelectTrigger>
                    <SelectContent onClick={(e) => e.stopPropagation()}>
                      {ASSIGNABLE_ACCESS_LEVELS.map((level) => (
                        <SelectItem key={level} value={level}>
                          {level}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {selected ? (
                    <Button
                      type="button"
                      size="sm"
                      variant={isCreator ? "secondary" : "destructive"}
                      className="h-8 px-2 text-xs shrink-0"
                      disabled={isCreator}
                      onClick={() => onToggle(memberId)}
                    >
                      <UserX className="h-3.5 w-3.5 mr-1" />
                      {isCreator ? "Locked" : "Remove"}
                    </Button>
                  ) : (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs shrink-0"
                      disabled={isCreator}
                      onClick={() => {
                        onAccessLevelChange(
                          memberId,
                          selectedAccessLevels[memberId.toLowerCase()] ??
                            "Editor",
                        );
                        onToggle(memberId);
                      }}
                    >
                      <UserPlus className="h-3.5 w-3.5 mr-1" />
                      Add
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </ScrollArea>
    </div>
  );
}
