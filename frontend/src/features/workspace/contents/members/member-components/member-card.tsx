import { Card } from "@/components/ui/card";
import type { MemberSummary } from "../members-type";
import { cn } from "@/lib/utils";
import { Checkbox } from "@/components/ui/checkbox";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { Mail, MoreVertical } from "lucide-react";
import { getRoleLabel } from "@/types/role";
import { Badge } from "@/components/ui/badge";

interface Props {
  member: MemberSummary;
  isEditMode: boolean;
  isSelected: boolean;
  onSelect: () => void;
  onDelete: (id: string) => void;
}

export function MemberCard({ member, isEditMode, isSelected, onSelect, onDelete }: Props) {
    const initials = member.name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    const roleLabel = getRoleLabel(member.role)
  return (
    <Card
      className={cn(
        "p-4 hover:shadow-md transition-all border-border",
        isEditMode && isSelected
          ? "bg-primary/10 border-primary ring-1 ring-primary"
          : "bg-card hover:bg-card/50",
        isEditMode && "cursor-pointer",
      )}
      onClick={isEditMode ? onSelect : undefined}
    >
      <div className="space-y-4">
        {/* Header with Checkbox and Avatar */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3 flex-1">
            {isEditMode && (
              <Checkbox
                checked={isSelected}
                onCheckedChange={onSelect}
                className="mt-1"
                onClick={(e) => e.stopPropagation()}
              />
            )}
            <Avatar className="h-10 w-10">
              <AvatarImage src={member.avatarUrl || "/placeholder.svg"} alt={member.name} />
              <AvatarFallback className="bg-primary text-primary-foreground font-semibold">
                {initials}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold text-sm text-foreground truncate">
                {member.name}
              </h3>
              <Badge className="text-xs text-muted-foreground mt-1">{roleLabel}</Badge>
            </div>
          </div>
          {!isEditMode && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem>
                  Tasks
                </DropdownMenuItem>
                <DropdownMenuItem>
                  Suspense
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => onDelete(member.id)}
                  className="text-destructive focus:text-destructive"
                >
                  Remove
                </DropdownMenuItem>


              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>

        {/* Contact Info */}
        <div className="space-y-2 border-t border-border pt-3">
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <Mail className="h-3.5 w-3.5 flex-shrink-0" />
            <a href={`mailto:${member.email}`} className="hover:text-primary truncate">
              {member.email}
            </a>
          </div>
        </div>

        {/* Join Date */}
        <div className="text-xs text-muted-foreground border-t border-border pt-3">
          Joined{" "}
          {new Date(member.joinedAt).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
          })}
        </div>
      </div>
    </Card>
  );
}
