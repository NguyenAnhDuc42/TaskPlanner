import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Users, Calendar, EllipsisVertical } from "lucide-react";
import { WorkspaceDetail } from "@/types/workspace";
import { UserSummaryType } from "@/components/custom/user-summary-type";
import { UserIconBar } from "@/components/custom/user-icon-bar";
import { RoleBadge } from "@/components/custom/role-badge";
import Link from "next/link";

interface WorkspaceCardProps {
  workspace: WorkspaceDetail;
}

export function WorkspaceDetailCard({ workspace }: WorkspaceCardProps) {
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric',
      year: 'numeric'
    });
  };

  return (
    <Link href={`/ws/${workspace.id}`}>
    <Card className="w-72 min-h-80 flex flex-col overflow-hidden hover:shadow-xl transition-all duration-300 bg-card border-border group hover:border-primary">
      {/* Color accent bar */}
      <div 
        className="h-1 w-full transition-all duration-300 group-hover:h-2"
        style={{ backgroundColor: workspace.color || 'hsl(var(--primary))' }}
      />
      
      <CardHeader className="flex-shrink-0 pb-0 px-6">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <h3 className="line-clamp-2 leading-tight text-card-foreground">{workspace.name}</h3>
            <EllipsisVertical className="text-muted-foreground"/>
          </div>
          <RoleBadge 
            role={workspace.yourRole} 
            className="w-fit capitalize border-primary text-primary hover:bg-primary hover:text-primary-foreground transition-colors" 
          />
        </div>
      </CardHeader>

      <CardContent className="flex flex-col pt-3 px-6 pb-6">
        {/* Description */}
        <div className="mb-4 h-16 flex items-start">
          <p className="text-muted-foreground leading-relaxed line-clamp-3">
            {workspace.description}
          </p>
        </div>

        {/* Owner info */}
        <div className="flex items-center gap-3 pb-4 mb-4 border-b border-border">
          <UserSummaryType 
            userSummary={workspace.owner} 
            styleDisplay="card"
            className="flex-1 min-w-0" 
          />
        </div>

        {/* Members section */}
        {(workspace.members && workspace.members.length > 0) && (
          <div className="mb-4">
            <div className="flex items-center gap-2 mb-3">
              <Users className="h-4 w-4 text-muted-foreground" />
              <span className="text-card-foreground">{workspace.memberCount} members</span>
            </div>
            
            <UserIconBar 
              users={workspace.members} 
              maxIcons={5} 
            />
          </div>
        )}

        {/* Creation date */}
        <div className="pt-4 mt-auto border-t border-border">
          <div className="flex items-center gap-2 text-muted-foreground">
            <Calendar className="h-4 w-4" />
            <span className="text-sm">Created {formatDate(workspace.createdAtUtc)}</span>
          </div>
        </div>
      </CardContent>
    </Card>
    </Link>
  );
}