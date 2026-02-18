import { useSidebarContext } from "@/features/workspace/components/sidebar-provider";
import { useHierarchy } from "../hierarchy/hierarchy-api";
import {
  Loader2,
  ChevronRight,
  Plus,
  MoreHorizontal,
  Lock,
} from "lucide-react";
import * as Icons from "lucide-react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useNavigate, useParams, useLocation } from "@tanstack/react-router";
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

const NAME_CHAR_LIMIT = 10;

function clampName(name: string, limit = NAME_CHAR_LIMIT) {
  if (name.length <= limit) return name;
  return `${name.slice(0, Math.max(0, limit - 1))}…`;
}

export function HierarchySidebar() {
  const { workspaceId } = useSidebarContext();
  const { data: hierarchy, isLoading, error } = useHierarchy(workspaceId || "");
  const [isCreateSpaceOpen, setIsCreateSpaceOpen] = useState(false);

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
      <div className="flex items-center justify-between px-3 py-2 group gap-2 min-w-0">
        <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider truncate flex-1 min-w-0">
          {hierarchy.name}
        </div>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-0.5">
          <DialogFormWrapper
            title="Create Space"
            open={isCreateSpaceOpen}
            onOpenChange={setIsCreateSpaceOpen}
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
            <CreateSpaceForm onSuccess={() => setIsCreateSpaceOpen(false)} />
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
        <div className="px-1 pb-4 flex flex-col gap-1 ">
          {hierarchy.spaces.map((space) => (
            <SpaceItem key={space.id} space={space} />
          ))}
          {hierarchy.spaces.length === 0 && (
            <div className="text-xs text-muted-foreground px-1 py-4 text-center italic">
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
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const { workspaceId } = useParams({ strict: false });
  const navigate = useNavigate();
  const location = useLocation();
  const isActive = location.pathname.includes(`/spaces/${space.id}`);

  const IconComponent = (Icons as any)[space.icon] || Icons.LayoutGrid;

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "flex items-center group/item w-full rounded-md transition-all duration-200 pr-1 border border-transparent overflow-hidden",
          isActive
            ? "bg-accent/40 border-current"
            : "hover:bg-accent/10 hover:border-current",
        )}
        style={{ color: space.color || "inherit" }}
      >
        <CollapsibleTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 ml-1 flex-shrink-0 hover:bg-muted"
            onClick={(e) => e.stopPropagation()}
          >
            <ChevronRight
              className={cn(
                "h-3.5 w-3.5 text-muted-foreground transition-transform duration-200",
                isOpen && "rotate-90",
              )}
            />
          </Button>
        </CollapsibleTrigger>

        <div
          className="flex-1 flex items-center gap-2 px-1 py-1.5 cursor-pointer min-w-0 overflow-hidden"
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })
          }
        >
          <IconComponent className="h-4 w-4 flex-shrink-0" />
          <span className="truncate text-sm font-medium text-foreground flex-1 min-w-0">
            {clampName(space.name)}
          </span>
          {space.isPrivate && (
            <Lock className="h-3 w-3 ml-1 flex-shrink-0 text-muted-foreground/50" />
          )}
        </div>

        <div className="flex-shrink-0 opacity-0 group-hover/item:opacity-100 transition-opacity px-1 flex items-center gap-0.5 flex-nowrap">
          <DialogFormWrapper
            title="Create Item"
            open={isCreateOpen}
            onOpenChange={setIsCreateOpen}
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
              onSuccess={() => {
                setIsOpen(true);
                setIsCreateOpen(false);
              }}
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
            <ItemSettingPopover
              type="Space"
              id={space.id}
              name={space.name}
              color={space.color}
              icon={space.icon}
              isPrivate={space.isPrivate}
            />
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
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const IconComponent = (Icons as any)[folder.icon] || Icons.Folder;
  const location = useLocation();
  const isActive = location.pathname.includes(`/folders/${folder.id}`);
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false });

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "flex items-center group/item w-full rounded-md transition-all duration-200 pr-1 border border-transparent overflow-hidden",
          isActive
            ? "bg-accent/40 border-current"
            : "hover:bg-accent/10 hover:border-current",
        )}
        style={{ color: folder.color || "inherit" }}
      >
        <CollapsibleTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-5 w-5 ml-1 flex-shrink-0 hover:bg-muted"
            onClick={(e) => e.stopPropagation()}
          >
            <ChevronRight
              className={cn(
                "h-3 w-3 text-muted-foreground transition-transform duration-200",
                isOpen && "rotate-90",
              )}
            />
          </Button>
        </CollapsibleTrigger>

        <div
          className="flex-1 flex items-center gap-2 px-1 py-1 cursor-pointer min-w-0 overflow-hidden"
          onClick={() =>
            navigate({ to: `/workspaces/${workspaceId}/folders/${folder.id}` })
          }
        >
          <IconComponent className="h-3.5 w-3.5 flex-shrink-0" />
          <span className="truncate text-xs text-foreground flex-1 min-w-0">
            {clampName(folder.name)}
          </span>
          {folder.isPrivate && (
            <Lock className="h-2.5 w-2.5 ml-1 flex-shrink-0 text-muted-foreground/50" />
          )}
        </div>

        <div className="flex-shrink-0 opacity-0 group-hover/item:opacity-100 transition-opacity px-1 flex items-center gap-0.5 flex-nowrap">
          <DialogFormWrapper
            title="Create List"
            open={isCreateOpen}
            onOpenChange={setIsCreateOpen}
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
              onSuccess={() => {
                setIsOpen(true);
                setIsCreateOpen(false);
              }}
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
              color={folder.color}
              icon={folder.icon}
              isPrivate={folder.isPrivate}
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

  const location = useLocation();
  const isActive = location.pathname.includes(`/lists/${list.id}`);
  const IconComponent = (Icons as any)[list.icon] || Icons.List;

  return (
    <div
      className={cn(
        "flex items-center group/list w-full rounded-md transition-all duration-200 pr-1 pl-7 border border-transparent overflow-hidden",
        isActive
          ? "bg-accent/40 border-current"
          : "hover:bg-accent/10 hover:border-current",
      )}
      style={{ color: list.color || "inherit" }}
    >
      <div
        className="flex-1 flex items-center gap-2 py-1 cursor-pointer min-w-0 overflow-hidden"
        onClick={() =>
          navigate({ to: `/workspaces/${workspaceId}/lists/${list.id}` })
        }
      >
        <div className="w-3.5 flex justify-center items-center flex-shrink-0">
          <IconComponent className="h-3.5 w-3.5 flex-shrink-0" />
        </div>
        <span className="truncate text-xs text-foreground flex-1 min-w-0">
          {clampName(list.name)}
        </span>
        {list.isPrivate && (
          <Lock className="h-2.5 w-2.5 ml-1 flex-shrink-0 text-muted-foreground/50" />
        )}
      </div>

      <div className="flex-shrink-0 ml-auto opacity-0 group-hover/list:opacity-100 transition-opacity gap-0.5 flex-nowrap">
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
          <ItemSettingPopover
            type="List"
            id={list.id}
            name={list.name}
            color={list.color}
            icon={list.icon}
            isPrivate={list.isPrivate}
          />
        </PopoverFormWrapper>
      </div>
    </div>
  );
}
