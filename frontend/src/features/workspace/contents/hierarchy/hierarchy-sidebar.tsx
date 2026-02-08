import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useHierarchy } from "../hierarchy/hierarchy-api";
import {
  Loader2,
  Folder,
  ChevronRight,
  Plus,
  MoreHorizontal,
} from "lucide-react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useState } from "react";
import type {
  SpaceHierarchy,
  FolderHierarchy,
  ListHierarchy,
} from "./hierarchy-type";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { PopoverFormWrapper } from "@/components/popover-wrapper";
import { CreateSpaceForm } from "./hierarchy-components/create-space-form";
import { CreateFolderListForm } from "./hierarchy-components/create-folderlist-form";
import { ItemSettingPopover } from "./hierarchy-components/item-setting.popover";

export function HierarchySidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: hierarchy, isLoading, error } = useHierarchy(workspaceId || "");

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-4 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin mr-2" />
        <span className="text-xs">Loading hierarchy...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 text-destructive text-xs">
        Failed to load hierarchy.
      </div>
    );
  }

  if (!hierarchy) return null;

  return (
    <div className="h-full flex flex-col">
      <div className="flex items-center justify-between px-3 py-2 group">
        <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider truncate">
          {hierarchy.name}
        </div>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-0.5">
          <DialogFormWrapper
            title="Create Space"
            trigger={
              <Button
                variant="ghost"
                size="icon"
                className="h-5 w-5 hover:bg-muted"
              >
                <Plus className="h-3.5 w-3.5 text-muted-foreground" />
              </Button>
            }
          >
            <CreateSpaceForm onSuccess={() => {}} />
          </DialogFormWrapper>

          {/* Global Workspace Settings could go here */}
          <Button
            variant="ghost"
            size="icon"
            className="h-5 w-5 hover:bg-muted"
          >
            <MoreHorizontal className="h-3.5 w-3.5 text-muted-foreground" />
          </Button>
        </div>
      </div>
      <ScrollArea className="flex-1">
        <div className="px-2 pb-4 flex flex-col gap-1">
          {hierarchy.spaces.map((space) => (
            <SpaceItem key={space.id} space={space} />
          ))}
          {hierarchy.spaces.length === 0 && (
            <div className="text-xs text-muted-foreground px-2 py-4 text-center italic">
              No spaces found.
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

function SpaceItem({ space }: { space: SpaceHierarchy }) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div className="flex items-center justify-between group/item w-full">
        <CollapsibleTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start h-8 px-2 text-sm font-medium hover:bg-accent/50 gap-2 overflow-hidden"
          >
            <ChevronRight
              className={cn(
                "h-3.5 w-3.5 text-muted-foreground transition-transform duration-200",
                isOpen && "rotate-90",
              )}
            />
            <div
              className="w-2 h-2 rounded-full flex-shrink-0"
              style={{ backgroundColor: space.color || "#808080" }}
            />
            <span className="truncate">{space.name}</span>
          </Button>
        </CollapsibleTrigger>

        <div className="opacity-0 group-hover/item:opacity-100 transition-opacity px-1 flex items-center">
          <DialogFormWrapper
            title="Create Item"
            trigger={
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6 hover:bg-muted"
              >
                <Plus className="h-3 w-3 text-muted-foreground" />
              </Button>
            }
          >
            <CreateFolderListForm
              parentId={space.id}
              parentType="Space"
              onSuccess={() => setIsOpen(true)}
            />
          </DialogFormWrapper>

          <PopoverFormWrapper
            trigger={
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6 hover:bg-muted"
              >
                <MoreHorizontal className="h-3 w-3 text-muted-foreground" />
              </Button>
            }
          >
            <ItemSettingPopover type="Space" id={space.id} name={space.name} />
          </PopoverFormWrapper>
        </div>
      </div>

      <CollapsibleContent className="space-y-1 mt-1 ml-3 pl-1 border-l border-border/40">
        {space.folders.map((folder) => (
          <FolderItem key={folder.id} folder={folder} />
        ))}
        {space.lists.map((list) => (
          <ListItem key={list.id} list={list} />
        ))}
        {space.folders.length === 0 && space.lists.length === 0 && (
          <div className="text-[10px] text-muted-foreground px-4 py-1 italic">
            Empty
          </div>
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}

function FolderItem({ folder }: { folder: FolderHierarchy }) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div className="flex items-center justify-between group/item w-full">
        <CollapsibleTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start h-7 px-2 text-xs hover:bg-accent/50 gap-2 overflow-hidden"
          >
            <ChevronRight
              className={cn(
                "h-3 w-3 text-muted-foreground transition-transform duration-200",
                isOpen && "rotate-90",
              )}
            />
            <Folder className="h-3.5 w-3.5 text-muted-foreground group-hover:text-foreground" />
            <span className="truncate">{folder.name}</span>
          </Button>
        </CollapsibleTrigger>

        <div className="opacity-0 group-hover/item:opacity-100 transition-opacity px-1 flex items-center">
          <DialogFormWrapper
            title="Create List"
            trigger={
              <Button
                variant="ghost"
                size="icon"
                className="h-5 w-5 hover:bg-muted"
              >
                <Plus className="h-3 w-3 text-muted-foreground" />
              </Button>
            }
          >
            <CreateFolderListForm
              parentId={folder.id}
              parentType="Folder"
              onSuccess={() => setIsOpen(true)}
            />
          </DialogFormWrapper>

          <PopoverFormWrapper
            trigger={
              <Button
                variant="ghost"
                size="icon"
                className="h-5 w-5 hover:bg-muted"
              >
                <MoreHorizontal className="h-3 w-3 text-muted-foreground" />
              </Button>
            }
          >
            <ItemSettingPopover
              type="Folder"
              id={folder.id}
              name={folder.name}
            />
          </PopoverFormWrapper>
        </div>
      </div>

      <CollapsibleContent className="space-y-1 mt-0.5 ml-3 pl-1 border-l border-border/40">
        {folder.lists.map((list) => (
          <ListItem key={list.id} list={list} />
        ))}
        {folder.lists.length === 0 && (
          <div className="text-[10px] text-muted-foreground px-4 py-1 italic">
            No lists
          </div>
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}

function ListItem({ list }: { list: ListHierarchy }) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false });

  return (
    <Button
      variant="ghost"
      size="sm"
      className="w-full justify-start h-7 px-2 text-xs hover:bg-accent/50 gap-2 group/list"
      onClick={() =>
        navigate({ to: `/workspaces/${workspaceId}/lists/${list.id}` })
      }
    >
      <div className="w-3.5 flex justify-center items-center">
        <div
          className="w-1.5 h-1.5 rounded-full"
          style={{ backgroundColor: list.color || "#808080" }}
        />
      </div>
      <span className="truncate">{list.name}</span>
      <div className="ml-auto opacity-0 group-hover/list:opacity-100 transition-opacity">
        <PopoverFormWrapper
          trigger={
            <Button
              variant="ghost"
              size="icon"
              className="h-5 w-5 hover:bg-muted"
              onClick={(e) => e.stopPropagation()}
            >
              <MoreHorizontal className="h-3 w-3 text-muted-foreground" />
            </Button>
          }
        >
          <ItemSettingPopover type="List" id={list.id} name={list.name} />
        </PopoverFormWrapper>
      </div>
    </Button>
  );
}
