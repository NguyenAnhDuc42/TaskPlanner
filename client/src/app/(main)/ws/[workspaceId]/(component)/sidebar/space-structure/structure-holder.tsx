import { useState } from "react";
import { Plus, ChevronRight, ChevronDown, Layers3 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { SpaceNode as SpaceNodeComponent } from "./space-node";
import type { SpaceNode } from "@/features/workspace/workspacetype";
import { CreateSpaceDialog } from "@/components/custom-form/buttons/create-spcace-dialog";
import { useWorkspaceId } from "@/utils/current-layer-id";

interface StructureProps {
  spaces: SpaceNode[];
  onAddNewSpace?: () => void;
  isSidebarCollapsed?: boolean; // New prop
}

export function Structure({ spaces, onAddNewSpace, isSidebarCollapsed }: StructureProps) { // Added new prop
  const workspaceId = useWorkspaceId();
  const [isSpacesExpanded, setIsSpacesExpanded] = useState(true);

  // Conditionally render based on isSidebarCollapsed
  if (isSidebarCollapsed) {
    return (
      <div className="flex justify-start py-2">
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 p-0 text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-foreground border border-transparent hover:border-sidebar-border transition-all duration-200"
          title="Spaces"
        >
          <Layers3 className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <div className={`${isSpacesExpanded ? 'border border-sidebar-border rounded-sm overflow-hidden' : 'border border-transparent rounded-sm'}`}>
      {/* Collapsible Spaces Header */}
      <Collapsible open={isSpacesExpanded} onOpenChange={setIsSpacesExpanded}>
        <CollapsibleTrigger asChild>
          <Button
            className={`w-full justify-between h-8 px-2 py-1 text-sm font-medium text-sidebar-foreground bg-transparent hover:rounded-sm hover:text-sidebar-foreground  hover:bg-sidebar-accent  border-0 focus-visible:ring-0 focus-visible:ring-offset-0 group ${isSpacesExpanded ? 'rounded-sm border-b' : 'rounded-sm'}`}
          >
            <div className="flex items-center gap-2">
              <Layers3 className="size-3.5" />
              <span>Spaces</span>
              <div className="opacity-0 group-hover:opacity-100">
                {isSpacesExpanded ? 
                  <ChevronDown className="size-3" /> : 
                  <ChevronRight className="size-3" />
                }
              </div>
            </div>
            <div className="opacity-0 group-hover:opacity-100">
              <CreateSpaceDialog workspaceId={workspaceId}>
                <div
                  role="button"
                  tabIndex={0}
                  aria-label="Create space"
                  className="inline-flex items-center justify-center size-6 hover:bg-sidebar-accent mr-1 rounded cursor-pointer focus:outline-none focus-visible:ring-2"
                >
                  <Plus className="size-3" />
                </div>
              </CreateSpaceDialog>
            </div>
          </Button>
        </CollapsibleTrigger>
        
        <CollapsibleContent>
          <div className="space-y-0.5 p-1 border-sidebar-border rounded-t-md">
            {/* Existing spaces */}
            {spaces.map((space) => (
              <SpaceNodeComponent
                key={space.id}
                space={space}
              />
            ))}
            
            {/* Add new space button */}
            <CreateSpaceDialog workspaceId={workspaceId}>
            <Button
              variant="ghost"
              className="w-full justify-start h-7 px-2 py-1 text-xs hover:bg-sidebar-accent text-sidebar-foreground/60 hover:text-sidebar-foreground rounded focus-visible:ring-0 focus-visible:ring-offset-0"
              onClick={onAddNewSpace}
            >
              <div className="flex items-center gap-2">
                <Plus className="size-3" />
                <span>Add new space</span>
              </div>
            </Button>
            </CreateSpaceDialog>
          </div>
        </CollapsibleContent>
      </Collapsible>
    </div>
  );
}