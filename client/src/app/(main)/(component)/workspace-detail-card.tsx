import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Users, Calendar, EllipsisVertical } from "lucide-react";
import { WorkspaceDetail } from "@/types/workspace";
import { UserSummaryType } from "@/components/custom/user-summary-type";
import { UserIconBar } from "@/components/custom/user-icon-bar";
import { RoleBadge } from "@/components/custom/role-badge";


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
    <Card className="w-80 min-h-80 flex flex-col overflow-hidden rounded-xl shadow-lg hover:shadow-2xl transition-all duration-300 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 group">
      <div 
        className="h-2 w-full"
        style={{ backgroundColor: workspace.color || '#6366f1' }}
      />
      
      <CardHeader className="flex-shrink-0 pt-4 pb-2 px-6">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-xl line-clamp-2 leading-tight text-gray-900 dark:text-white">{workspace.name}</h3>
            <EllipsisVertical/>
          </div>
          <RoleBadge role={workspace.yourRole}/>
        </div>
      </CardHeader>

      <CardContent className="flex-grow flex flex-col pt-2 px-6 pb-6">
        <div className="mb-4 h-16 flex items-start">
          <p className="text-gray-600 dark:text-gray-400 leading-relaxed line-clamp-3">
            {workspace.description}
          </p>
        </div>

        <div className="space-y-4">
            <UserSummaryType userSummary={workspace.owner} styleDisplay="card" />

            {(workspace.members && workspace.members.length > 0) && (
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <Users className="h-4 w-4 text-gray-500 dark:text-gray-400" />
                  <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{workspace.memberCount} members</span>
                </div>
                <div className="pl-1">
                    <UserIconBar users={workspace.members} maxIcons={7} />
                </div>
              </div>
            )}
        </div>

        <div className="flex-grow" />

        <div className="pt-4 mt-4 border-t border-gray-200 dark:border-gray-800">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
            <Calendar className="h-4 w-4" />
            <span className="text-sm">Created {formatDate(workspace.createdAtUtc)}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

