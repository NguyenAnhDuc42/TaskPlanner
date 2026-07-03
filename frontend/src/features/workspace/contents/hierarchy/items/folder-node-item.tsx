import { useState } from "react";
import { observer } from "mobx-react-lite";
import { ChevronRight, MoreVertical } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { cn } from "@/lib/utils";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { useStore } from "@/stores/root.store";
import { SortableItem } from "../dnd/sortable-item";
import { NodeTasksList } from "./task-node-list";
import { EntityLayerType as EntityLayerConst } from "@/types/entity-layer-type";
import { FolderContextMenu } from "../hierarchy-components/context-menus/folder-context-menu";
import { EntityMenuTrigger } from "../hierarchy-components/context-menus/shared";

interface FolderNodeItemProps {
  folderId: string;
  spaceId: string;
}

export const FolderNodeItem = observer(function FolderNodeItem({
  folderId,
  spaceId,
}: FolderNodeItemProps) {
  const rootStore = useStore();
  const folder = rootStore.folderStore.getById(folderId);
  const hasTasks = rootStore.taskStore.all.some((t) => t.folderId === folderId && !t.parentTaskId);

  const [isOpen, setIsOpen] = useState(false);

  const location = useLocation();
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();


  if (!folder) return null;

  const isActive = location.pathname.includes(`/folders/${folder.id}`);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <SortableItem
        id={`folder-${folder.id}`}
        data={{
          ...folder,
          type: EntityLayerConst.ProjectFolder,
          id: folder.id,
          parentId: spaceId,
          spaceId: spaceId,
        }}
      >
        <FolderContextMenu
          folderId={folder.id}
          folderName={folder.name}
        >
          <div
            className={cn(
              "flex items-center px-1 py-0.5 rounded-md transition-colors mb-px group border",
              isActive
                ? "bg-primary/5 text-primary border-primary/25"
                : "text-muted-foreground border-transparent hover:bg-muted/50 hover:text-foreground hover:border-border/30",
            )}
          >
            <button
              type="button"
              className="relative flex items-center justify-center w-5 h-5 shrink-0 cursor-pointer rounded-sm hover:bg-background/50 group/icon mr-1.5"
              onClick={(e) => {
                if (!hasTasks) return;
                e.stopPropagation();
                setIsOpen(!isOpen);
              }}
            >
              <DynamicIcon
                name={folder.icon || "Folder"}
                size={14}
                color={folder.color}
                className={cn(
                  "absolute transition-none",
                  hasTasks && "group-hover/icon:opacity-0",
                )}
              />
              {hasTasks && (
                <ChevronRight
                  className={cn(
                    "h-4 w-4 absolute opacity-0 text-muted-foreground group-hover/icon:opacity-100 transition-none",
                    isOpen && "rotate-90",
                  )}
                />
              )}
            </button>

            <button
              type="button"
              className="flex-1 text-left text-[11px] font-semibold outline-none select-none whitespace-nowrap"
              onClick={() => navigate({
                  to: "/workspaces/$workspaceId/folders/$folderId",
                  params: { workspaceId, folderId: folder.id },
                })
              }
            >
              {folder.name}
            </button>

            <div className="flex items-center gap-0.5 ml-1 shrink-0">

              <EntityMenuTrigger>
                <button
                  type="button"
                  className="h-4 w-4 p-0.5 flex items-center justify-center rounded-sm hover:bg-muted-foreground/10 text-muted-foreground hover:text-primary transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreVertical className="h-3.5 w-3.5" />
                </button>
              </EntityMenuTrigger>
            </div>
          </div>
        </FolderContextMenu>
      </SortableItem>
      <CollapsibleContent className="overflow-hidden">
        <div className="ml-3.5 pl-2 border-l border-border flex flex-col">
          <NodeTasksList
            nodeId={folder.id}
            parentType={EntityLayerConst.ProjectFolder}
            isExpanded={isOpen}
            spaceId={spaceId}
          />
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
});
