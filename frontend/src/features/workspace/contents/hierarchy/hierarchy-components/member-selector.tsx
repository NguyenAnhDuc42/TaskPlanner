import { useState, useMemo } from "react";
import { useMembers } from "../../members/members-api";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Check, Search, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import type { MemberSummary } from "../../members/members-type";
import { useAuth } from "@/features/auth/auth-context";

interface Props {
  workspaceId: string;
  selectedIds: string[];
  onToggle: (id: string) => void;
  onRemove: (id: string) => void;
}

export function MemberSelector({
  workspaceId,
  selectedIds,
  onToggle,
  onRemove,
}: Props) {
  const { user } = useAuth();
  const [search, setSearch] = useState("");
  const { data: membersPack, isLoading } = useMembers(workspaceId, {
    name: search || undefined,
  });

  const allMembers = useMemo(() => {
    const members = membersPack?.pages.flatMap((page: any) => page.items) || [];
    // Filter out current user (creator/current member)
    return members.filter((m: MemberSummary) => m.id !== user?.id);
  }, [membersPack, user?.id]);

  return (
    <div className="space-y-3">
      {selectedIds.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-2 p-2 border border-dashed rounded-md bg-muted/30">
          {selectedIds.map((id) => {
            const member = allMembers.find((m: MemberSummary) => m.id === id);
            return (
              <Badge
                key={id}
                variant="secondary"
                className="gap-1 pl-1 py-1 h-7"
              >
                <Avatar className="h-5 w-5">
                  <AvatarImage src={member?.avatarUrl} />
                  <AvatarFallback className="text-[10px]">
                    {member?.name?.charAt(0) || "U"}
                  </AvatarFallback>
                </Avatar>
                <span className="max-w-[100px] truncate">
                  {member?.name || "Selected Member"}
                </span>
                <button
                  type="button"
                  onClick={() => onRemove(id)}
                  className="ml-1 ring-offset-background rounded-full outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                >
                  <X className="h-3 w-3 text-muted-foreground hover:text-foreground" />
                </button>
              </Badge>
            );
          })}
        </div>
      )}

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search workspace members..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9 h-9"
        />
      </div>

      <ScrollArea className="h-[180px] border rounded-md p-1">
        {isLoading ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            Loading members...
          </div>
        ) : allMembers.length === 0 ? (
          <div className="p-4 text-center text-xs text-muted-foreground">
            No members found.
          </div>
        ) : (
          <div className="space-y-1">
            {allMembers.map((member: MemberSummary) => (
              <button
                key={member.id}
                type="button"
                onClick={() => onToggle(member.id)}
                className={cn(
                  "w-full flex items-center gap-2 px-2 py-1.5 rounded-sm text-sm transition-colors",
                  selectedIds.includes(member.id)
                    ? "bg-primary/10 text-primary"
                    : "hover:bg-muted",
                )}
              >
                <Avatar className="h-7 w-7">
                  <AvatarImage src={member.avatarUrl} />
                  <AvatarFallback>
                    {member.name?.charAt(0) || "U"}
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1 text-left">
                  <div className="font-medium truncate">{member.name}</div>
                  <div className="text-[10px] text-muted-foreground truncate">
                    {member.email}
                  </div>
                </div>
                {selectedIds.includes(member.id) && (
                  <Check className="h-4 w-4" />
                )}
              </button>
            ))}
          </div>
        )}
      </ScrollArea>
    </div>
  );
}
