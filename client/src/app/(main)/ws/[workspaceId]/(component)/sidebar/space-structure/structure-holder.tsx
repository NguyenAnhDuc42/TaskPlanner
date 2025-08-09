import { useState } from "react";
import { Plus, ChevronRight, ChevronDown, Layers3 } from "lucide-react";

import { SpaceNode as SpaceNodeComponent } from "./space-node";
import type { SpaceNode } from "@/features/workspace/workspacetype";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Button } from "@/components/ui/button";

interface StructureProps {
  spaces: SpaceNode[];
  onAddNewSpace?: () => void;
}

export function Structure({ spaces, onAddNewSpace }: StructureProps) {
  const [isSpacesExpanded, setIsSpacesExpanded] = useState(true);

  return (
    <div className={`${isSpacesExpanded ? 'border border-sidebar-border rounded-lg overflow-hidden' : ''}`}>
      {/* Collapsible Spaces Header */}
      <Collapsible open={isSpacesExpanded} onOpenChange={setIsSpacesExpanded}>
        <CollapsibleTrigger asChild>
          <Button
            variant="ghost"
            className={`w-full justify-between h-8 px-3 py-1 text-xs font-medium text-sidebar-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/50 border-0 focus-visible:ring-0 focus-visible:ring-offset-0 group ${isSpacesExpanded ? 'rounded-t-lg rounded-b-none' : 'rounded-lg'}`}
          >
            <div className="flex items-center gap-2">
              <Layers3 className="size-3.5" />
              <span>SPACES</span>
              <div className="opacity-0 group-hover:opacity-100">
                {isSpacesExpanded ? 
                  <ChevronDown className="size-3" /> : 
                  <ChevronRight className="size-3" />
                }
              </div>
            </div>
            <div className="opacity-0 group-hover:opacity-100">
              <Plus className="size-3" />
            </div>
          </Button>
        </CollapsibleTrigger>
        
        <CollapsibleContent>
          <div className="space-y-0.5 p-1 border-t border-sidebar-border rounded-t-md">
            {/* Existing spaces */}
            {spaces.map((space) => (
              <SpaceNodeComponent
                key={space.id}
                space={space}
              />
            ))}
            
            {/* Add new space button */}
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
          </div>
        </CollapsibleContent>
      </Collapsible>
    </div>
  );
}