import { Button } from "@/components/ui/button";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { FolderNode } from "@/features/workspace/workspacetype";
import { cn } from "@/lib/utils";
import { Folder, ChevronRight, MoreHorizontal, Plus } from "lucide-react";
import React from "react";
import { ListContext, ListItem } from "./list-item";
import { SpaceContext } from "./space-item";
import { useCreateList } from "@/features/list/list-hooks";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Loader2 } from "lucide-react";
import { useWorkspaceStore } from "@/utils/workspace-store";

export type FolderContext = SpaceContext & { folderId: string; }
interface FolderItemProps { 
  folder: FolderNode;
  context: FolderContext;
  isHighlighted?: boolean;
}

function CreateListForm({ folderContext, onSuccess } : { folderContext: FolderContext; onSuccess: () => void;}) {
  const { mutate, isPending, isError, error } = useCreateList();
  const [name, setName] = React.useState("");
  const [icon, setIcon] = React.useState("");
  const { selectedWorkspaceId } = useWorkspaceStore();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    
    if (!selectedWorkspaceId) {
      console.error("Workspace ID is missing");
      return;
    }
    
    mutate(
      { 
        workspaceId : selectedWorkspaceId,
        spaceId: folderContext.spaceId,
        folderId: folderContext.folderId,
        name,
        icon: icon || undefined
      },
      {
        onSuccess: () => {
          setName("");
          setIcon("");
          onSuccess();
        },
      }
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input
        className="border rounded px-3 py-2"
        placeholder="List name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <input
        className="border rounded px-3 py-2"
        placeholder="Icon URL (optional)"
        value={icon}
        onChange={(e) => setIcon(e.target.value)}
      />
      <Button type="submit" disabled={isPending} className="mt-2">
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : "Create List"}
      </Button>
      {isError && (
        <div className="text-destructive text-sm mt-2">
          {error?.detail || "Failed to create list. Please try again."}
        </div>
      )}
    </form>
  );
}

export function FolderItem({ folder, context, isHighlighted }: FolderItemProps) {
  const [isOpen, setIsOpen] = React.useState(true);
  const [isHovered, setIsHovered] = React.useState(false);
  const [showCreateListModal, setShowCreateListModal] = React.useState(false);
  
  const listContext: ListContext = {
    ...context, // Includes spaceId and folderId
    listId: "" // Will be set per list
  };

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div className={cn("group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer","hover:bg-neutral-800",
        isHighlighted && "bg-neutral-800",)}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}>
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            <Folder className={cn("absolute h-4 w-4 text-yellow-500 transition-opacity",isHovered ? "opacity-0" : "opacity-100",)} />
            <ChevronRight className={cn("absolute h-4 w-4 text-neutral-400 transition-transform",
                isOpen ? "rotate-90" : "", isHovered ? "opacity-100" : "opacity-0", )}/>
          </div>
        </CollapsibleTrigger>
        <span className="flex-1 text-sm font-medium text-neutral-200 truncate">{folder.name}</span>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <MoreHorizontal className="h-4 w-4" />
          </Button>
          
          <Dialog open={showCreateListModal} onOpenChange={setShowCreateListModal}>
            <DialogTrigger asChild>
              <Button 
                variant="ghost" 
                size="sm" 
                className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700"
                onClick={() => setShowCreateListModal(true)}
              >
                <Plus className="h-4 w-4" />
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create List in {folder.name}</DialogTitle>
              </DialogHeader>
              <CreateListForm 
                folderContext={context} 
                onSuccess={() => setShowCreateListModal(false)} 
              />
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <CollapsibleContent>
        <div className="relative pl-6 before:absolute before:left-[20px] before:top-0 before:bottom-0 before:w-px before:bg-neutral-700">
          <div className="space-y-0.5">
            {folder.lists.map((list) => (
              <ListItem 
                key={list.id} 
                list={list} 
                context={{ ...listContext, listId: list.id }}
                isHighlighted={isHighlighted && list.id === "list-2"} 
              />
            ))}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}