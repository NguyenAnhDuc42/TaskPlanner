import React from "react";
import * as Icons from "lucide-react";
import {
  ChevronRight,
  FolderPlus,
  Plus,
  MoreHorizontal,
  Lock,
} from "lucide-react";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-provider";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { DropdownWrapper } from "@/components/dropdown-wrapper";
import { FolderForm } from "../hierarchy-components/creation-form/folder-form";
import { TaskForm } from "../hierarchy-components/creation-form/task-form";
import { SpaceMenu } from "../hierarchy-components/dropdown/space-menu";
import { SortableItem } from "../dnd/sortable-item";
import { FolderItem } from "./folder-item";
import { NodeTasksList } from "./node-tasks-list";
import { clampName } from "../utils/name-utils";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import type { SpaceHierarchy } from "../hierarchy-type";

interface SpaceItemProps {
  space: SpaceHierarchy;
  isForcedOpen?: boolean;
}

export function SpaceItem({ space, isForcedOpen }: SpaceItemProps) {
  const [isOpen, setIsOpen] = React.useState(false);
  const { workspaceId } = useWorkspace();
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = location.pathname.includes(`/spaces/${space.id}`);
  const IconComponent = (Icons as any)[space.icon] || Icons.LayoutGrid;
  const spaceColor = space.color || "var(--primary)";
  const effectiveOpen = isForcedOpen || isOpen;

  return (
    <Collapsible
      open={effectiveOpen}
      onOpenChange={setIsOpen}
      className="w-full"
    >
      <SortableItem
        id={`space-${space.id}`}
        data={{
          ...space, // Entire object for high-fidelity DragOverlay
          type: EntityLayerConst.ProjectSpace,
          id: space.id,
          orderKey: space.orderKey,
        }}
      >
        <div
          className={cn(
            "flex items-center w-full px-1 py-0.5 rounded-sm transition-colors cursor-pointer mb-px group",
            isActive
              ? "bg-primary/10 text-primary"
              : "text-foreground hover:bg-muted",
          )}
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })
          }
        >
          <div
            className="relative flex items-center justify-center w-5 h-5 flex-shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-0.5"
            onClick={(e) => {
              e.stopPropagation();
              setIsOpen(!isOpen);
            }}
          >
            <IconComponent
              className="h-3.5 w-3.5 absolute transition-opacity group-hover/icon:opacity-0"
              style={{ color: isActive ? spaceColor : undefined }}
            />
            <ChevronRight
              className={cn(
                "h-4 w-4 absolute opacity-0 transition-all text-muted-foreground group-hover/icon:opacity-100",
                isOpen && "rotate-90",
              )}
            />
          </div>
          <span className="truncate text-[11px] font-bold flex-1">
            {clampName(space.name)}
          </span>
          <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
            <DialogFormWrapper
              title="Create New Folder"
              trigger={
                <button
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <FolderPlus className="h-3.5 w-3.5" />
                </button>
              }
              contentClassName="sm:max-w-[800px] p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
            >
              <FolderForm
                workspaceId={workspaceId || ""}
                spaceId={space.id}
                onSubmitSuccess={() => {}}
                onCancel={() => {}}
              />
            </DialogFormWrapper>

            <DialogFormWrapper
              title="Create New Task"
              trigger={
                <button
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <Plus className="h-3.5 w-3.5" />
                </button>
              }
              contentClassName="max-w-3xl p-0 overflow-hidden border-none shadow-2xl rounded-2xl bg-background outline-none ring-1 ring-border/50"
            >
              <TaskForm
                workspaceId={workspaceId || ""}
                parentId={space.id}
                parentType={EntityLayerConst.ProjectSpace}
                onSubmitSuccess={() => {}}
                onCancel={() => {}}
              />
            </DialogFormWrapper>

            <DropdownWrapper
              align="start"
              side="right"
              trigger={
                <button
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreHorizontal className="h-3.5 w-3.5" />
                </button>
              }
            >
              <SpaceMenu spaceId={space.id} />
            </DropdownWrapper>
          </div>
          {space.isPrivate && (
            <Lock className="h-3 w-3 text-muted-foreground flex-shrink-0 opacity-40 ml-1" />
          )}
        </div>
      </SortableItem>

      <CollapsibleContent className="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up">
        <div className="ml-3 pl-1 border-l border-border mt-0.5 flex flex-col">
          <SortableContext
            items={space.folders.map((f) => `folder-${f.id}`)}
            strategy={verticalListSortingStrategy}
          >
            {space.folders.map((folder) => (
              <FolderItem key={folder.id} folder={folder} spaceId={space.id} />
            ))}
          </SortableContext>
          <NodeTasksList
            nodeId={space.id}
            parentType={EntityLayerConst.ProjectSpace}
            isExpanded={effectiveOpen}
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}
