import { Button } from "@/components/ui/button";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { SpaceNode } from "@/features/workspace/workspacetype";
import { cn } from "@/lib/utils";
import { ChevronRight, LayoutGrid, MoreHorizontal, Plus } from "lucide-react";
import React from "react";
import { FolderItem } from "./folder-item";
import { ListItem } from "./list-item";
import { useCreateFolder } from "@/features/folder/folder-hooks";
import { useCreateList } from "@/features/list/list-hooks";

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Loader2 } from "lucide-react";
import { useWorkspaceStore } from "@/utils/workspace-store";

export type SpaceContext =  { spaceId : string };
interface SpaceItemProps { space: SpaceNode ,context: SpaceContext}

interface CreateFolderOrListFormProps {
  spaceId: string;
  onSuccess: () => void;
}

function CreateFolderOrListForm({ spaceId, onSuccess }: CreateFolderOrListFormProps) {
  const [selectedType, setSelectedType] = React.useState<"folder" | "list">("folder");
  const [name, setName] = React.useState("");
  
  const { mutate: createFolder, isPending: isCreatingFolder } = useCreateFolder();
  const { mutate: createList, isPending: isCreatingList } = useCreateList();
  const { selectedWorkspaceId } = useWorkspaceStore();
  
  const isPending = isCreatingFolder || isCreatingList;

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    
    if (selectedType === "folder") {
      createFolder({
        workspaceId: selectedWorkspaceId!,
        spaceId: spaceId,
        name
      }, {
        onSuccess: () => {
          setName("");
          onSuccess();
        }
      });
    } else {
      createList({
        workspaceId: selectedWorkspaceId!,
        spaceId: spaceId,
        name
      }, {
        onSuccess: () => {
          setName("");
          onSuccess();
        }
      });
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <div className="flex gap-2 mb-2">
        <Button
          type="button"
          variant={selectedType === "folder" ? "default" : "outline"}
          className="flex-1"
          onClick={() => setSelectedType("folder")}
        >
          Folder
        </Button>
        <Button
          type="button"
          variant={selectedType === "list" ? "default" : "outline"}
          className="flex-1"
          onClick={() => setSelectedType("list")}
        >
          List
        </Button>
      </div>
      
      <input
        className="border rounded px-3 py-2"
        placeholder={`${selectedType === "folder" ? "Folder" : "List"} name`}
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      
      <Button 
        type="submit" 
        disabled={isPending} 
        className="mt-2"
      >
        {isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Creating...
          </>
        ) : (
          `Create ${selectedType === "folder" ? "Folder" : "List"}`
        )}
      </Button>
    </form>
  );
}

export function SpaceItem({ space, context }: SpaceItemProps) {
  const [isOpen, setIsOpen] = React.useState(true);
  const [isHovered, setIsHovered] = React.useState(false);
  const [showCreateModal, setShowCreateModal] = React.useState(false);
  
  const hasChildren = (space.folders && space.folders.length > 0) || 
                      (space.directLists && space.directLists.length > 0);
  const SpaceIcon = LayoutGrid;

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div
        className={cn("group flex items-center gap-2 py-1 px-2 rounded-sm cursor-pointer hover:bg-neutral-800")}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}>
        <CollapsibleTrigger asChild>
          <div className="relative w-6 h-6 flex items-center justify-center flex-shrink-0">
            <SpaceIcon className={cn("absolute h-4 w-4 text-blue-500 transition-opacity",
                isHovered ? "opacity-0" : "opacity-100",)}/>
            {hasChildren && (
              <ChevronRight className={cn("absolute h-4 w-4 text-neutral-400 transition-transform ",
                  isOpen ? "rotate-90" : "",isHovered ? "opacity-100" : "opacity-0", )}/>
            )}
          </div>
        </CollapsibleTrigger>

        <span className="flex-1 font-semibold text-neutral-200 truncate">{space.name}</span>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700">
            <MoreHorizontal className="h-4 w-4" />
          </Button>
          
          <Dialog open={showCreateModal} onOpenChange={setShowCreateModal}>
            <DialogTrigger asChild>
              <Button 
                variant="ghost" 
                size="sm" 
                className="h-6 w-6 p-0 text-neutral-400 hover:bg-neutral-700"
                onClick={() => setShowCreateModal(true)}
              >
                <Plus className="h-4 w-4" />
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>
                  Create in {space.name}
                </DialogTitle>
              </DialogHeader>
              <CreateFolderOrListForm 
                spaceId={context.spaceId} 
                onSuccess={() => setShowCreateModal(false)} 
              />
            </DialogContent>
          </Dialog>
        </div>
      </div>
      {hasChildren && (
        <CollapsibleContent>
          <div className="relative pl-6 before:absolute before:left-[20px] before:top-0 before:bottom-0 before:w-px before:bg-neutral-700">
            <div className="space-y-0.5">
              {space.folders?.map((folder) => (
               <FolderItem 
                  key={folder.id} 
                  folder={folder} 
                  context={{ 
                    spaceId: context.spaceId, 
                    folderId: folder.id 
                  }} 
                />
              ))}
              {space.directLists?.map((list) => (
                <ListItem 
                  key={list.id} 
                  list={list} 
                  context={{ 
                    spaceId: context.spaceId,
                    folderId: undefined, 
                    listId: list.id 
                  }} 
                />
              ))}
            </div>
          </div>
        </CollapsibleContent>
      )}
    </Collapsible>
  )
}