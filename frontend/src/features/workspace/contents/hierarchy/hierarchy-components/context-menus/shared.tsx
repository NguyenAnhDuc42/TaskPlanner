import React, { createContext, useContext } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

export interface EntityMenuContextType {
  renderMenuItems: (isContext: boolean) => React.ReactNode;
}

export const EntityMenuContext = createContext<EntityMenuContextType | null>(null);

export interface DeleteConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  onConfirm: () => void;
}

export function DeleteConfirmationDialog({
  open,
  onOpenChange,
  title,
  description,
  onConfirm,
}: DeleteConfirmationDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent size="sm">
        <AlertDialogHeader>
          <AlertDialogTitle className="text-sm font-bold">{title}</AlertDialogTitle>
          <AlertDialogDescription className="text-xs text-muted-foreground">
            {description}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="mt-2">
          <AlertDialogCancel size="sm" className="text-xs cursor-pointer">Cancel</AlertDialogCancel>
          <AlertDialogAction 
            size="sm" 
            variant="destructive" 
            className="text-xs cursor-pointer"
            onClick={(e) => {
              e.stopPropagation();
              onConfirm();
            }}
          >
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

export function EntityMenuTrigger({ children }: { children: React.ReactNode }) {
  const context = useContext(EntityMenuContext);
  if (!context) throw new Error("EntityMenuTrigger must be used within EntityContextMenu");

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {children}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" side="right" className="w-52 bg-background/95 backdrop-blur-md border-border/50 shadow-2xl rounded-xl p-1.5 animate-in fade-in-0 zoom-in-95">
        {context.renderMenuItems(false)}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
