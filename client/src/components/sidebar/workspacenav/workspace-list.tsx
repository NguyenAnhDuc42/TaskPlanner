import * as React from "react";
import { MoreHorizontal, Plus } from "lucide-react";
import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Workspace } from "@/features/workspace/workspacetype";
import { AddWorkspaceButton } from "./add-workspace-button";


interface WorkspaceListProps {
  workspaces: Workspace[];
  onSelect: (id: string) => void;
}

export function WorkspaceList({ workspaces, onSelect }: WorkspaceListProps) {
  const [isAddDialogOpen, setIsAddDialogOpen] = React.useState(false);

  if (workspaces.length === 0) {
    return (
      <DropdownMenuItem onClick={() => setIsAddDialogOpen(true)}>
        <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
          <Plus className="size-4" />
        </div>
        <div className="text-muted-foreground font-medium">Add workspace</div>
      </DropdownMenuItem>
    );
  }

  return (
    <>
      <div className="max-h-48 overflow-y-auto">
        {workspaces.slice(0, 5).map((workspace) => (
          <DropdownMenuItem
            key={workspace.id}
            onClick={() => onSelect(workspace.id)}
          >
            <div className="flex size-6 items-center justify-center rounded-md border">
              {workspace.icon ? (
                <img src={workspace.icon} alt={workspace.name} className="size-4" />
              ) : (
                <div className="size-3.5 shrink-0" />
              )}
            </div>
            <span className="truncate ml-2">{workspace.name}</span>
          </DropdownMenuItem>
        ))}
      </div>
      
      {workspaces.length > 5 && (
        <AllWorkspacesDialog 
          workspaces={workspaces} 
          onSelect={onSelect} 
        />
      )}
      
      <DropdownMenuSeparator />
      <AddWorkspaceButton 
        isOpen={isAddDialogOpen}
        onOpenChange={setIsAddDialogOpen}
      />
    </>
  );
}

interface AllWorkspacesDialogProps {
  workspaces: Workspace[];
  onSelect: (id: string) => void;
}

function AllWorkspacesDialog({ workspaces, onSelect }: AllWorkspacesDialogProps) {
     const [isAddDialogOpen, setIsAddDialogOpen] = React.useState(false);
  return (
    <Dialog>
      <DialogTrigger asChild>
        <DropdownMenuItem onSelect={(e) => e.preventDefault()}>
          <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
            <MoreHorizontal className="size-4" />
          </div>
          <div className="text-muted-foreground font-medium">
            Show all workspaces
          </div>
        </DropdownMenuItem>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>All Workspaces</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-2 max-h-96 overflow-y-auto">
          {workspaces.map((workspace) => (
            <DialogClose asChild key={workspace.id}>
              <div
                className="flex items-center gap-2 p-2 border rounded hover:bg-accent cursor-pointer"
                onClick={() => onSelect(workspace.id)}
              >
                {workspace.icon ? (
                  <img
                    src={workspace.icon}
                    alt={workspace.name}
                    className="size-5"
                  />
                ) : (
                  <div className="size-5" />
                )}
                <span className="truncate">{workspace.name}</span>
              </div>
            </DialogClose>
          ))}
        </div>
        <div className="mt-4">
           <AddWorkspaceButton 
            isOpen={isAddDialogOpen}
            onOpenChange={setIsAddDialogOpen}/>
        </div>
        <DialogClose asChild>
          <Button variant="secondary">Close</Button>
        </DialogClose>
      </DialogContent>
    </Dialog>
  );
}