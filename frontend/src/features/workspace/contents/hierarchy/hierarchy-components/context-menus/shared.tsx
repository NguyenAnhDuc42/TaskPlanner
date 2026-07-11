import React, { createContext, useContext } from "react";
import { Pencil } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuSub,
  DropdownMenuSubTrigger,
  DropdownMenuSubContent,
} from "@/components/ui/dropdown-menu";
import {
  ContextMenuSub,
  ContextMenuSubTrigger,
  ContextMenuSubContent,
} from "@/components/ui/context-menu";
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
import { DebouncedInput } from "@/components/debounced-input";
import { PickerPanel } from "@/components/universal-picker";

export interface EntityMenuContextType {
  renderMenuItems: (isContext: boolean) => React.ReactNode;
}

// eslint-disable-next-line react-refresh/only-export-components
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
      <DropdownMenuTrigger asChild onPointerDown={(e) => e.stopPropagation()}>
        {children}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        side="right"
        onCloseAutoFocus={(e) => e.preventDefault()}
        className="w-52 bg-popover border-border shadow-md rounded-md p-1 animate-in fade-in-0 zoom-in-95"
      >
        {context.renderMenuItems(false)}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export interface EditFieldsSubmenuProps {
  isContext: boolean;
  name: string;
  icon: string;
  color: string;
  onRename: (name: string) => void;
  onIconColorChange: (icon: string, color: string) => void;
}

// Shared "Edit" submenu (rename + icon/color) for Space/Folder/Task context menus. Uses
// PickerPanel directly instead of UniversalPicker/IconColorPicker — those wrap the picker in a
// Radix Popover, and a Popover's portaled content registers as an "outside click" to a wrapping
// DropdownMenu/ContextMenu, closing the whole menu the instant it opens. PickerPanel is the same
// picker UI with no Popover dependency, so it can live directly inside Sub content.
export function EditFieldsSubmenu({
  isContext,
  name,
  icon,
  color,
  onRename,
  onIconColorChange,
}: EditFieldsSubmenuProps) {
  const Sub = isContext ? ContextMenuSub : DropdownMenuSub;
  const SubTrigger = isContext ? ContextMenuSubTrigger : DropdownMenuSubTrigger;
  const SubContent = isContext ? ContextMenuSubContent : DropdownMenuSubContent;

  return (
    <Sub>
      <SubTrigger className="gap-2 cursor-pointer">
        <Pencil className="h-3.5 w-3.5" />
        <span>Edit</span>
      </SubTrigger>
      <SubContent
        className="p-0 border-none shadow-none bg-transparent w-fit"
      >
        {/* Radix menu content intercepts keydown at this level for its own roving-focus/typeahead
            navigation (Enter activates the focused item, Space/printable keys drive typeahead
            search) — without stopping propagation here, typing in the name field or the picker's
            hex/search inputs below gets swallowed by that navigation instead of reaching the
            input (Enter "selecting" a menu item, Space never actually typing a space). */}
        <div className="flex flex-col" onKeyDown={(e) => e.stopPropagation()}>
          <div className="w-80 p-2 bg-popover border border-border border-b-0 shadow-md rounded-t-md">
            <DebouncedInput
              value={name}
              onChange={onRename}
              onPointerDown={(e) => e.stopPropagation()}
              placeholder="Name"
              className="w-full h-7 text-[11px] font-semibold px-2 rounded-md border border-border/40 bg-muted/20 outline-none focus:border-primary/50 text-foreground"
            />
          </div>
          <PickerPanel
            icon={icon}
            color={color}
            colors={["#6366f1", "#06b6d4", "#10b981", "#f59e0b", "#ef4444", "#ec4899", "#8b5cf6", "#14b8a6", "#f97316", "#84cc16"]}
            onSelect={onIconColorChange}
          />
        </div>
      </SubContent>
    </Sub>
  );
}
