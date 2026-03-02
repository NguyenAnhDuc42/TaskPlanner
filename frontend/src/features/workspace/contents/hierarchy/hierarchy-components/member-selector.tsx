import { useState, useMemo } from "react";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Search,
  UserPlus,
  UserX,
  Shield,
  ShieldCheck,
  ShieldAlert,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
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
import type { EntityAccessMember } from "../hierarchy-type";

interface Props {
  members: EntityAccessMember[];
  isLoading: boolean;
  selectedIds: string[];
  selectedAccessLevels: Record<string, AssignableAccessLevel>;
  creatorWorkspaceMemberId?: string;
  onToggle: (id: string) => void;
  onAccessLevelChange: (id: string, level: AssignableAccessLevel) => void;
}

export function MemberSelector({
  members,
  isLoading,
  selectedIds,
  selectedAccessLevels,
  creatorWorkspaceMemberId,
  onToggle,
  onAccessLevelChange,
}: Props) {
  const [search, setSearch] = useState("");

  const filteredMembers = useMemo(() => {
    return members.filter(
      (m) =>
        m.userName.toLowerCase().includes(search.toLowerCase()) ||
        m.userEmail.toLowerCase().includes(search.toLowerCase()),
    );
  }, [members, search]);

  const sortedMembers = useMemo(() => {
    return [...filteredMembers].sort((a, b) => {
      const aSelected = selectedIds.includes(a.workspaceMemberId.toLowerCase());
      const bSelected = selectedIds.includes(b.workspaceMemberId.toLowerCase());

      if (aSelected !== bSelected) return aSelected ? -1 : 1;

      const aIsCreator =
        creatorWorkspaceMemberId?.toLowerCase() ===
        a.workspaceMemberId.toLowerCase();
      const bIsCreator =
        creatorWorkspaceMemberId?.toLowerCase() ===
        b.workspaceMemberId.toLowerCase();
      if (aIsCreator !== bIsCreator) return aIsCreator ? -1 : 1;

      return (a.userName || "").localeCompare(b.userName || "");
    });
  }, [filteredMembers, selectedIds, creatorWorkspaceMemberId]);

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

      <ScrollArea className="h-[300px] border rounded-md p-2">
        {isLoading ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            Loading members...
          </div>
        ) : members.length === 0 ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            No members found.
          </div>
        ) : (
          <div className="space-y-2">
            {sortedMembers.map((member) => {
              const memberId = member.workspaceMemberId.toLowerCase();
              const selected = selectedIds.includes(memberId);
              const isCreator =
                creatorWorkspaceMemberId?.toLowerCase() === memberId;
              const isInherited = member.isInherited;
              const role = member.role;

              return (
                <div
                  key={memberId}
                  className={cn(
                    "w-full flex items-center gap-2 px-2 py-2 rounded-md border text-sm transition-colors",
                    selected
                      ? "bg-primary/5 border-primary/20 text-foreground"
                      : "bg-card border-border hover:bg-muted",
                    isInherited && "border-dashed",
                  )}
                >
                  <div className="flex-1 min-w-0 flex items-center gap-3">
                    <div className="flex-1 text-left min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-medium truncate">
                          {member.userName}
                        </span>
                        {role === "Owner" && (
                          <ShieldAlert className="h-3 w-3 text-red-500" />
                        )}
                        {role === "Admin" && (
                          <ShieldCheck className="h-3 w-3 text-blue-500" />
                        )}
                        {role === "Member" && (
                          <Shield className="h-3 w-3 text-slate-500" />
                        )}
                        <span className="text-[9px] text-muted-foreground font-normal">
                          ({role})
                        </span>
                      </div>
                      <div className="text-[10px] text-muted-foreground truncate">
                        {member.userEmail}
                      </div>
                      <div className="flex items-center gap-2 mt-0.5">
                        {member.isMe && (
                          <Badge
                            variant="outline"
                            className="px-1 py-0 text-[8px] h-3.5 border-blue-200 text-blue-600 uppercase"
                          >
                            You
                          </Badge>
                        )}
                        {isCreator && (
                          <Badge
                            variant="secondary"
                            className="px-1 py-0 text-[8px] h-3.5 uppercase"
                          >
                            Creator
                          </Badge>
                        )}
                        {isInherited && (
                          <Badge
                            variant="outline"
                            className="px-1 py-0 text-[8px] h-3.5 border-dashed text-orange-500 border-orange-200 uppercase"
                          >
                            Inherited
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>

                  <Select
                    value={selectedAccessLevels[memberId] ?? "Editor"}
                    onValueChange={(value) =>
                      onAccessLevelChange(
                        memberId,
                        value as AssignableAccessLevel,
                      )
                    }
                    disabled={!selected || isCreator}
                  >
                    <SelectTrigger
                      size="sm"
                      className="w-[92px] shrink-0 h-8"
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
                      variant={isCreator ? "ghost" : "destructive"}
                      className="h-8 px-2 text-xs shrink-0"
                      disabled={isCreator || isInherited}
                      onClick={() => onToggle(memberId)}
                    >
                      <UserX className="h-3.5 w-3.5" />
                      {isCreator || isInherited ? "" : ""}
                    </Button>
                  ) : (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="h-8 px-2 text-xs shrink-0"
                      onClick={() => {
                        onAccessLevelChange(
                          memberId,
                          selectedAccessLevels[memberId] ?? "Editor",
                        );
                        onToggle(memberId);
                      }}
                    >
                      <UserPlus className="h-3.5 w-3.5" />
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
