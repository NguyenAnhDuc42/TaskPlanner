"use client";

import * as React from "react";
import { ChevronsUpDown, Plus, Loader2, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from "@/components/ui/sidebar";
import {
  useSidebarWorkspaces,
  useCreateWorkspace,
} from "@/features/workspace/workspace-hooks";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogClose,
} from "@/components/ui/dialog";

export function WorkspaceSwitcher() {
  const { isMobile } = useSidebar();
  const { data, isLoading } = useSidebarWorkspaces();
  const workspaces = data?.workspaces || [];
  const [activeTeam, setActiveTeam] = React.useState(workspaces[0]);
  const [isAddWorkspaceDialogOpen, setIsAddWorkspaceDialogOpen] =
    React.useState(false);

  React.useEffect(() => {
    if (workspaces.length && !activeTeam) {
      setActiveTeam(workspaces[0]);
    }
  }, [workspaces, activeTeam]);

  if (!activeTeam) {
    return null;
  }

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton
              size="lg"
              className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
            >
              <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
                {activeTeam.icon ? (
                  <ChevronsUpDown className="size-4" />
                ) : (
                  <ChevronsUpDown className="size-4" />
                )}
              </div>
              <div className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-medium">{activeTeam.name}</span>
              </div>
              <ChevronsUpDown className="ml-auto" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg"
            align="start"
            side={isMobile ? "bottom" : "right"}
            sideOffset={4}
          >
            <DropdownMenuLabel className="text-muted-foreground text-xs">
              Workspaces
            </DropdownMenuLabel>
            {isLoading ? (
              <div className="flex justify-center items-center py-4">
                <Loader2 className="animate-spin h-5 w-5 text-muted-foreground" />
              </div>
            ) : (
              <>
                <div className="max-h-48 overflow-y-auto">
                  {workspaces.slice(0, 5).map((workspace) => (
                    <DropdownMenuItem
                      key={workspace.name}
                      onClick={() => setActiveTeam(workspace)}
                    >
                      <div className="flex size-6 items-center justify-center rounded-md border">
                        {workspace.icon ? (
                          <img src={"/images/logo.png"} alt={workspace.name} />
                        ) : (
                          <ChevronsUpDown className="size-3.5 shrink-0" />
                        )}
                      </div>
                      {workspace.name}
                    </DropdownMenuItem>
                  ))}
                </div>
                {workspaces.length > 5 && (
                  <Dialog>
                    <DialogTrigger asChild>
                      <DropdownMenuItem onSelect={(e) => e.preventDefault()}>
                        <div className="flex size-6 items-center justify-center rounded-md border bg-transparent">
                          <MoreHorizontal className="size-4" />
                        </div>
                        <div className="text-muted-foreground font-medium">
                          Show all workspaces
                        </div>
                      </DropdownMenuItem>
                    </DialogTrigger>
                    <DialogContent>
                      <DialogHeader>
                        <DialogTitle>All Workspaces</DialogTitle>
                      </DialogHeader>
                      <div className="flex flex-col gap-2 max-h-96 overflow-y-auto">
                        {workspaces.map((workspace) => (
                          <DialogClose asChild key={workspace.id}>
                            <div
                              className="flex items-center gap-2 p-2 border rounded hover:bg-accent cursor-pointer"
                              onClick={() => {
                                setActiveTeam(workspace);
                              }}
                            >
                              {workspace.icon ? (
                                <img
                                  src={"/images/logo.png"}
                                  alt={workspace.name}
                                  className="size-5"
                                />
                              ) : (
                                <ChevronsUpDown className="size-5" />
                              )}
                              <span>{workspace.name}</span>
                            </div>
                          </DialogClose>
                        ))}
                      </div>
                      <AddWorkspaceButton
                        isOpen={isAddWorkspaceDialogOpen}
                        onOpenChange={setIsAddWorkspaceDialogOpen}
                      />
                      <DialogClose asChild>
                        <Button variant="secondary">Close</Button>
                      </DialogClose>
                    </DialogContent>
                  </Dialog>
                )}
              </>
            )}
            <DropdownMenuSeparator />
            <AddWorkspaceButton
              isOpen={isAddWorkspaceDialogOpen}
              onOpenChange={setIsAddWorkspaceDialogOpen}
            />
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}

interface AddWorkspaceButtonProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

function AddWorkspaceButton({ isOpen, onOpenChange }: AddWorkspaceButtonProps) {
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

interface AddWorkspaceFormProps {
  onSuccess: () => void;
}

function AddWorkspaceForm({ onSuccess }: AddWorkspaceFormProps) {
  const { mutate, isPending, isSuccess, isError, error, reset } = useCreateWorkspace();
  const [name, setName] = React.useState("");
  const [description, setDescription] = React.useState("");
  const [icon, setIcon] = React.useState("");
  const [color, setColor] = React.useState("");
  const [isPrivate, setIsPrivate] = React.useState(false);
  const [submitted, setSubmitted] = React.useState(false);

  React.useEffect(() => {
    if (isSuccess && submitted) {
      setName("");
      setDescription("");
      setIcon("");
      setColor("");
      setIsPrivate(false);
      setSubmitted(false);
      reset();
      onSuccess();
    }
  }, [isSuccess, submitted, reset, onSuccess]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitted(true);
    mutate({ name, description, icon, color, isPrivate });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <input
        className="border rounded px-2 py-1"
        placeholder="Workspace name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <textarea
        className="border rounded px-2 py-1"
        placeholder="Description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <input
        className="border rounded px-2 py-1"
        placeholder="Icon URL (optional)"
        value={icon}
        onChange={(e) => setIcon(e.target.value)}
      />
      <input
        className="border rounded px-2 py-1"
        placeholder="Color (optional)"
        value={color}
        onChange={(e) => setColor(e.target.value)}
      />
      <label className="flex items-center gap-2">
        <input
          type="checkbox"
          checked={isPrivate}
          onChange={(e) => setIsPrivate(e.target.checked)}
        />
        Private
      </label>
      <Button type="submit" disabled={isPending}>
        {isPending ? "Creating..." : "Create Workspace"}
      </Button>
      {isError && (
        <div className="text-destructive text-sm">
          {error?.detail || "Failed to create workspace."}
        </div>
      )}
    </form>
  );
}