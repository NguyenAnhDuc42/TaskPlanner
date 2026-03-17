import { 
  Users, 
  Share2, 
  Lock, 
  BarChart3, 
  FileText,
  MousePointer2,
  Database
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { WorkspaceSummary } from '../type';
import { useNavigate } from '@tanstack/react-router';
import { RoleBadge } from '@/components/role-badge';

interface WorkspaceDetailsProps {
  workspace: WorkspaceSummary | null
}

export function WorkspaceDetails({ workspace }: WorkspaceDetailsProps) {
  const navigate = useNavigate();

  if (!workspace) {
    return (
      <Card className="w-96 flex flex-col items-center justify-center p-6 bg-muted/5 border-dashed rounded-md">
        <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center mb-4">
          <MousePointer2 className="h-6 w-6 text-muted-foreground/40" />
        </div>
        <p className="text-sm text-muted-foreground text-center">
          Select a workspace to view detailed analytics and management options.
        </p>
      </Card>
    );
  }

  const handleOpenWorkspace = () => {
    navigate({
      to: "/workspaces/$workspaceId",
      params: { workspaceId: workspace.id }
    });
  };

  return (
    <Card className="w-96 p-6 overflow-y-auto h-full flex flex-col shadow-sm border border-border/50 outline rounded-md">
      {/* Header */}
      <div className="mb-6 flex items-start justify-between">
        <div className="min-w-0 pr-4">
          <h2 className="text-xl font-bold text-foreground truncate">{workspace.name}</h2>
          <p className="text-sm text-muted-foreground line-clamp-2">{workspace.description || "No description provided."}</p>
        </div>
        <Button 
          variant="outline" 
          className="shrink-0 hover:bg-primary hover:text-primary-foreground transition-all px-2 rounded-md"
          onClick={handleOpenWorkspace}
          title="Open Workspace"
        >
          Open
        </Button>
      </div>

      {/* Quick Stats - Combined current data + placeholders */}
      <div className="mb-6 grid grid-cols-2 gap-3">
        <Card className="border-border/50 bg-background p-3 shadow-none rounded-md ">
          <div className="flex items-center gap-2 mb-1">
            <FileText className="h-3 w-3 text-muted-foreground" />
            <p className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground">Documents</p>
          </div>
          <p className="text-lg font-bold text-foreground">--</p>
        </Card>
        <Card className="border-border/50 bg-background p-3 shadow-none rounded-md">
          <div className="flex items-center gap-2 mb-1">
            <Database className="h-3 w-3 text-muted-foreground" />
            <p className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground">Storage</p>
          </div>
          <p className="text-lg font-bold text-foreground">0.0 GB</p>
        </Card>
        <Card className="border-border/50 bg-background p-3 shadow-none rounded-md">
          <div className="flex items-center gap-2 mb-1">
            <BarChart3 className="h-3 w-3 text-muted-foreground" />
            <p className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground">Activities</p>
          </div>
          <p className="text-lg font-bold text-foreground">--</p>
        </Card>
        <Card className="border-border/50 bg-background p-3 shadow-none rounded-md">
          <div className="flex items-center gap-2 mb-1">
            <Users className="h-3 w-3 text-muted-foreground" />
            <p className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground">Members</p>
          </div>
          <p className="text-lg font-bold text-foreground">{workspace.memberCount}</p>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="members" className="w-full flex-1 flex flex-col min-h-0">
        <TabsList className="grid w-full grid-cols-3 bg-muted/50 p-1 rounded-sm">
          <TabsTrigger value="members" className="gap-2 text-[10px] uppercase font-bold tracking-wider transition-all data-[state=active]:bg-background data-[state=active]:shadow-sm rounded-sm">
            <Users className="h-3 w-3" />
            Members
          </TabsTrigger>
          <TabsTrigger value="settings" className="gap-2 text-[10px] uppercase font-bold tracking-wider transition-all data-[state=active]:bg-background data-[state=active]:shadow-sm rounded-sm">
            <Lock className="h-3 w-3" />
            Access
          </TabsTrigger>
          <TabsTrigger value="activity" className="gap-2 text-[10px] uppercase font-bold tracking-wider transition-all data-[state=active]:bg-background data-[state=active]:shadow-sm rounded-sm">
            <BarChart3 className="h-3 w-3" />
            Log
          </TabsTrigger>
        </TabsList>

        <TabsContent value="members" className="mt-4 flex-1 flex flex-col min-h-0">
          <div className="space-y-2 overflow-y-auto flex-1 pr-1 bg-muted/30 ">
             {workspace.members && workspace.members.map(member => (
               <div key={member.id} className="flex items-center justify-between p-3 rounded-sm bg-muted/30 border border-transparent hover:border-border/50 transition-all group">
                  <div className="flex items-center gap-3">
                    <div className="h-8 w-8 rounded-full bg-primary/10 border border-primary/20 flex items-center justify-center text-[10px] font-black text-primary uppercase">
                      {member.name.substring(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <p className="text-xs font-bold text-foreground">{member.name}</p>
                      <div className="mt-1">
                        <RoleBadge role={member.role} />
                      </div>
                    </div>
                  </div>
                </div>
             ))}
             {workspace.memberCount > 5 ? (
               <p className="text-center py-2 text-[10px] font-mono text-muted-foreground/40 uppercase tracking-[0.2em] italic">
                 + {workspace.memberCount - 5} more members...
               </p>
             ) : workspace.memberCount === 0 ? (
               <p className="text-center py-8 text-[10px] font-mono text-muted-foreground/40 uppercase tracking-[0.2em] italic">
                 No members found.
               </p>
             ) : null}
          </div>
          <Button className="w-full gap-2 mt-4 font-bold text-[10px] uppercase tracking-widest h-10 shadow-lg shadow-primary/10 rounded-md" variant="secondary" onClick={handleOpenWorkspace}>
            <Users className="h-3.5 w-3.5" />
            Manage Members
          </Button>
        </TabsContent>

        <TabsContent value="settings" className="mt-4 space-y-3">
          <Card className="border-border/50 bg-muted/20 p-4 shadow-none">
            <div className="flex items-center justify-between mb-2">
              <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Role Visibility</p>
              <div className="px-2 py-0.5 rounded-sm bg-primary/10 text-primary text-[9px] font-black uppercase">
                {workspace.role}
              </div>
            </div>
            <div className="flex items-center gap-2 text-[10px] text-muted-foreground font-medium italic">
              <Lock className="h-3 w-3" />
              Permissions derived from workspace role.
            </div>
          </Card>
          <Button className="w-full gap-2 font-bold text-[10px] uppercase tracking-widest h-10 transition-all hover:bg-primary hover:text-primary-foreground" variant="outline" onClick={handleOpenWorkspace}>
            <Share2 className="h-3.5 w-3.5" />
            Security Settings
          </Button>
        </TabsContent>

        <TabsContent value="activity" className="mt-4 flex-1 flex flex-col">
          <div className="flex-1 flex flex-col items-center justify-center border border-dashed border-border/50 rounded-sm bg-muted/10">
             <BarChart3 className="h-8 w-8 text-muted-foreground/20 mb-2" />
             <p className="text-muted-foreground/40 text-[10px] font-mono font-black uppercase tracking-[0.3em] text-center px-12 leading-relaxed">
               No telemetry captured for this node.
             </p>
          </div>
        </TabsContent>
      </Tabs>
    </Card>
  )
}
