import * as React from "react";
import { Plus } from "lucide-react";
import { DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { AddWorkspaceForm } from "./add-workspace-form";


interface AddWorkspaceButtonProps {
  isOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
}

export function AddWorkspaceButton({ 
  isOpen = false, 
  onOpenChange = () => {} 
}: AddWorkspaceButtonProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <DropdownMenuItem onSelect={(e) => e.preventDefault()}>
          <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
            <Plus className="size-4" />
          </div>
          <div className="text-muted-foreground font-medium">Add workspace</div>
        </DropdownMenuItem>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add Workspace</DialogTitle>
        </DialogHeader>
        <AddWorkspaceForm onSuccess={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}