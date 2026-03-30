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
} from "@/components/ui/collapsible";
import { cn } from "@/lib/utils";
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

const NAME_CHAR_LIMIT = 14;

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
      <div className="flex flex-col items-center justify-center h-40 text-muted-foreground/40 gap-3">
        <Loader2 className="h-5 w-5 animate-spin" />
        <span className="text-[10px] font-black uppercase tracking-widest">Loading...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6 text-destructive/60 text-[10px] font-black uppercase tracking-widest text-center">
        Error loading mission data.
      </div>
    );
  }

  if (!hierarchy) return null;

  return (
    <div className="h-full flex flex-col bg-transparent rounded-md backdrop-blur-sm">
      <div className="flex items-center justify-between px-4 py-3 group gap-2 min-w-0">
        <div className="text-[10px] font-black text-muted-foreground/60 uppercase tracking-[0.2em] truncate flex-1 min-w-0">
          {hierarchy.name}
        </div>
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
          <DialogFormWrapper
            title="Create Space"
            open={isCreateSpaceOpen}
            onOpenChange={setIsCreateSpaceOpen}
            trigger={
              <div
                className="p-1 rounded-md hover:bg-white/10 cursor-pointer transition-colors"
                role="button"
              >
                <Plus className="h-3.5 w-3.5 text-muted-foreground/60" />
              </div>
            }
          >
            <CreateSpaceForm onSuccess={() => setIsCreateSpaceOpen(false)} />
          </DialogFormWrapper>
        </div>
      </div>
      <ScrollArea className="flex-1">
        <div className="px-1 pb-4 flex flex-col gap-0">
          {hierarchy.spaces.map((space) => (
            <SpaceItem key={space.id} space={space} />
          ))}
          {hierarchy.spaces.length === 0 && (
            <div className="text-[10px] text-muted-foreground/30 px-4 py-8 text-center italic font-bold uppercase tracking-widest underline decoration-2 decoration-white/5 underline-offset-8">
              No regions discovered
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
  const spaceColor = space.color || "var(--primary)";

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "group/item relative flex items-center w-full px-1 py-1 rounded-md transition-all duration-300 cursor-pointer overflow-hidden mb-0",
          isActive
            ? "bg-white/[0.08] shadow-[inset_0_1px_1px_rgba(255,255,255,0.05)] border border-white/10"
            : "hover:bg-white/5 border border-transparent hover:border-white/5",
        )}
        onClick={() =>
          navigate({ to: `/workspaces/${workspaceId}/spaces/${space.id}` })
        }
      >
        <div
          className={cn(
            "p-1 rounded-md mr-1 transition-transform duration-300 hover:bg-white/10",
            isOpen && "rotate-90",
          )}
          onClick={(e) => {
            e.stopPropagation();
            setIsOpen(!isOpen);
          }}
        >
          <ChevronRight className="h-3 w-3 text-muted-foreground/30" />
        </div>

        <div
          className="flex-shrink-0 p-1 rounded-md shadow-sm border border-white/5"
          style={{
            backgroundColor: `${spaceColor}15`,
            color: spaceColor,
          }}
        >
          <IconComponent className="h-3.5 w-3.5" />
        </div>

        <span
          className={cn(
            "ml-3 truncate text-[13px] font-bold tracking-tight transition-colors duration-300",
            isActive ? "text-foreground" : "text-muted-foreground/60 group-hover/item:text-foreground/90",
          )}
        >
          {clampName(space.name, 14)}
        </span>

        {space.isPrivate && (
          <Lock className="h-3 w-3 ml-2 text-muted-foreground/20" />
        )}

        <div className="ml-auto flex items-center gap-0.5 opacity-0 group-hover/item:opacity-100 transition-opacity">
          <div onClick={(e) => e.stopPropagation()}>
            <DialogFormWrapper
              title="Create Item"
              open={isCreateOpen}
              onOpenChange={setIsCreateOpen}
              trigger={
                <div
                  className="p-1 rounded-md hover:bg-white/10 transition-colors"
                >
                  <Plus className="h-3 w-3 text-muted-foreground/40" />
                </div>
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
          </div>

          <div onClick={(e) => e.stopPropagation()}>
            <PopoverFormWrapper
              trigger={
                <div
                  className="p-1 rounded-md hover:bg-white/10 transition-colors"
                >
                  <MoreHorizontal className="h-3 w-3 text-muted-foreground/40" />
                </div>
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
      </div>

      <CollapsibleContent className="space-y-0.5 mt-0.5 ml-3.5 pl-1 border-l border-white/5 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:slide-out-to-top-2 data-[state=open]:slide-in-from-top-2 duration-300">
        {space.folders.map((folder) => (
          <FolderItem key={folder.id} folder={folder} />
        ))}
        {space.lists.map((list) => (
          <ListItem key={list.id} list={list} />
        ))}
        {space.folders.length === 0 && space.lists.length === 0 && (
          <div className="text-[10px] text-muted-foreground/20 px-6 py-2 italic font-medium uppercase tracking-widest">
            Void
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

  const folderColor = folder.color || "var(--primary)";

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="w-full">
      <div
        className={cn(
          "group/item relative flex items-center w-full px-1 py-0.5 rounded-md transition-all duration-300 cursor-pointer overflow-hidden",
          isActive 
            ? "bg-white/[0.08] border border-white/10" 
            : "hover:bg-white/5 border border-transparent hover:border-white/5",
        )}
        onClick={() =>
          navigate({ to: `/workspaces/${workspaceId}/folders/${folder.id}` })
        }
      >
        <div
          className={cn(
            "p-1 rounded-md mr-0.5 transition-transform duration-200 hover:bg-white/10",
            isOpen && "rotate-90",
          )}
          onClick={(e) => {
            e.stopPropagation();
            setIsOpen(!isOpen);
          }}
        >
          <ChevronRight className="h-3 w-3 text-muted-foreground/30" />
        </div>

        <div
          className="flex-shrink-0 p-1 rounded-sm opacity-70 group-hover/item:opacity-100 transition-opacity"
          style={{ color: folderColor }}
        >
          <IconComponent className="h-3.5 w-3.5" />
        </div>

        <span
          className={cn(
            "ml-2 truncate text-[11px] font-bold tracking-tight transition-colors duration-300",
            isActive ? "text-foreground" : "text-muted-foreground/60 group-hover/item:text-foreground/90",
          )}
        >
          {clampName(folder.name)}
        </span>

        <div className="ml-auto flex items-center gap-0.5 opacity-0 group-hover/item:opacity-100 transition-opacity">
          <div onClick={(e) => e.stopPropagation()}>
            <DialogFormWrapper
              title="Create List"
              open={isCreateOpen}
              onOpenChange={setIsCreateOpen}
              trigger={
                <div
                  className="p-1 rounded-md hover:bg-white/10 transition-colors"
                >
                  <Plus className="h-3 w-3 text-muted-foreground/40" />
                </div>
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
          </div>

          <div onClick={(e) => e.stopPropagation()}>
            <PopoverFormWrapper
              trigger={
                <div
                  className="p-1 rounded-md hover:bg-white/10 transition-colors"
                >
                  <MoreHorizontal className="h-3 w-3 text-muted-foreground/40" />
                </div>
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
      </div>

      <CollapsibleContent className="space-y-0.5 mt-0.5 ml-3 pl-1 border-l border-white/5 duration-300">
        {folder.lists.map((list) => (
          <ListItem key={list.id} list={list} />
        ))}
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
  const listColor = list.color || "var(--primary)";

  return (
    <div
      className={cn("group/list relative flex items-center w-full pr-2 pl-6 py-0.5 rounded-md transition-all duration-300 cursor-pointer overflow-hidden",
        isActive ? "bg-white/[0.08] border border-white/10" 
                 : "hover:bg-white/5 border border-transparent hover:border-white/5",
      )}
      onClick={() => navigate({ to: `/workspaces/${workspaceId}/lists/${list.id}` })}
    >
      <div className="w-3.5 flex justify-center items-center flex-shrink-0 transition-all duration-300 group-hover/list:scale-110 opacity-60 group-hover/list:opacity-100"
           style={{ color: listColor }}
      >
        <IconComponent className="h-3.5 w-3.5" />
      </div>

      <span
        className={cn(
          "ml-3 truncate text-[11px] font-bold tracking-tight transition-colors duration-300",
          isActive ? "text-foreground" : "text-muted-foreground/50 group-hover/list:text-foreground/90",
        )}
      >
        {clampName(list.name)}
      </span>

      {list.isPrivate && (
        <Lock className="h-2.5 w-2.5 ml-1.5 text-muted-foreground/20" />
      )}

      <div className="ml-auto opacity-0 group-hover/list:opacity-100 transition-opacity">
        <div onClick={(e) => e.stopPropagation()}>
          <PopoverFormWrapper
            trigger={
              <div
                className="p-1 rounded-md hover:bg-white/10"
              >
                <MoreHorizontal className="h-3 w-3 text-muted-foreground/40" />
              </div>
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
    </div>
  );
}
