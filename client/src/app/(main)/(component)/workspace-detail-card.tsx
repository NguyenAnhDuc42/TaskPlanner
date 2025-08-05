import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Users, Calendar, EllipsisVertical } from "lucide-react";
import { WorkspaceDetail } from "@/types/workspace";
import { UserSummaryType } from "@/components/custom/user-summary-type";
import { UserIconBar } from "@/components/custom/user-icon-bar";
import { RoleBadge } from "@/components/custom/role-badge";
import Link from "next/link";
import { ScrollArea } from "@/components/ui/scroll-area";

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
      <Card className="w-72 min-h-80 p-0 flex flex-col overflow-hidden transform hover:scale-[1.01] transition-all duration-300 bg-background border-border group hover:border-primary origin-top">
        {/* Color accent bar */}
        <div 
          className="h-3 w-full transition-all duration-300 group-hover:h-5"
          style={{ backgroundColor: workspace.color || '#ffffff' }}
        />
        
        <CardHeader className="flex-shrink-0 pb-0 px-6">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h1 className="line-clamp-2 leading-tight text-card-foreground font-bold text-xl break-words hyphens-auto max-w-[200px]">
                {workspace.name}
              </h1>
              <EllipsisVertical className="text-muted-foreground flex-shrink-0"/>
            </div>
            <RoleBadge 
              role={workspace.yourRole} 
              className="w-fit capitalize  text-primary  hover:text-primary-foreground transition-colors" 
            />
          </div>
        </CardHeader>

        <CardContent className="flex flex-col pt-3 px-6 pb-6 flex-1 min-h-0">
          {/* Description with proper scrolling */}
          <div className="mb-4 flex-shrink-0">
            <ScrollArea className="h-16 w-full">
              <div className="pr-4">
                <p 
                  className="text-muted-foreground leading-relaxed text-sm break-words hyphens-auto"
                  style={{
                    wordWrap: 'break-word',
                    overflowWrap: 'break-word',
                    wordBreak: 'break-word',
                    whiteSpace: 'pre-wrap',
                    maxWidth: '100%'
                  }}
                >
                  {workspace.description}
                </p>
              </div>
            </ScrollArea>
          </div>

          {/* Owner info */}
          <div className="flex items-center gap-3 pb-4 mb-4 border-b border-border flex-shrink-0">
            <div className="flex-1 min-w-0 overflow-hidden">
              <UserSummaryType 
                userSummary={workspace.owner} 
                styleDisplay="card"
                className="w-full" 
              />
            </div>
          </div>

          {/* Members section */}
          {(workspace.members && workspace.members.length > 0) && (
            <div className="mb-4 flex-shrink-0">
              <div className="flex items-center gap-2 mb-3">
                <Users className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                <span className="text-card-foreground text-sm break-words">
                  {workspace.memberCount} members
                </span>
              </div>
              
              <div className="w-full overflow-hidden">
                <UserIconBar 
                  users={workspace.members} 
                  maxIcons={5} 
                />
              </div>
            </div>
          )}

          {/* Creation date */}
          <div className="pt-4 mt-auto border-t border-border flex-shrink-0">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Calendar className="h-4 w-4 flex-shrink-0" />
              <span className="text-sm break-words">
                Created {formatDate(workspace.createdAtUtc)}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}