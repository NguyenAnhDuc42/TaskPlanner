import * as React from "react";
import { EntityViewFrame } from "../entity-view-frame";
import { SpaceBoard } from "./space-components/space-board";
import { useGetSpaceDetailQuery, useSpaceDetail, useGetSpaceItemsQuery } from "./space-api";
import { DynamicIcon } from "@/components/dynamic-icon";
import { ChevronRight, GitMerge } from "lucide-react";
import { useParams } from "@tanstack/react-router";
import { CreateStatusForm } from "@/features/workspace/components/forms/create-status-form";

interface SpaceViewProps {
  spaceId: string;
}

export function SpaceView({ spaceId }: SpaceViewProps) {
  const { workspaceId } = useParams({ strict: false }) as { workspaceId: string };
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  
  // Load space details & items into Redux
  const { isLoading: isDetailLoading } = useGetSpaceDetailQuery(spaceId);
  const space = useSpaceDetail(spaceId);
  const { data: spaceItems } = useGetSpaceItemsQuery(spaceId);
  const statuses = spaceItems?.statuses || [];

  return (
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full h-full">
          {/* Left: Breadcrumbs */}
          <div className="flex items-center gap-0.5 text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest h-full">
            <span className="hover:text-foreground transition-colors cursor-pointer">
              Workspace
            </span>
            <ChevronRight className="h-2.5 w-2.5 opacity-40 mx-0.5" />
            <div className="flex items-center gap-1.5 text-foreground/70 h-full">
              {space && (
                <>
                  <DynamicIcon
                    name={space.icon || "Folder"}
                    size={12}
                    color={space.color || "#3b82f6"}
                    className="stroke-[2.5]"
                  />
                  <span className="font-black tracking-tight text-foreground/90">
                    {space.name}
                  </span>
                </>
              )}
            </div>
          </div>
          <div>{/* Actions */}</div>
        </div>
      }
      subHeader={
        <div className="flex gap-4">
          <button className="text-sm font-bold border-b-2 border-primary text-primary pb-2 mt-2">
            Board
          </button>
        </div>
      }
    >
      <div className="h-full w-full flex flex-col bg-background/50">
        <SpaceBoard spaceId={spaceId} onWorkflowOpen={() => setIsWorkflowOpen(true)} />
      </div>

      {space?.workflowId && (
        <CreateStatusForm
          isOpen={isWorkflowOpen}
          onClose={() => setIsWorkflowOpen(false)}
          workflowId={space.workflowId}
          currentStatuses={statuses}
        />
      )}
    </EntityViewFrame>
  );
}
