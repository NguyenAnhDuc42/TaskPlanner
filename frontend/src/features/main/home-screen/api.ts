import * as React from "react";
import { toast } from "sonner";
import type { z } from "zod";
import type { createWorkspaceSchema } from "./type";
import { useStore } from "@/stores/root.store";
import { useUser } from "@/features/auth/auth-api";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import { signalRService } from "@/lib/signalr-service";

type CreateWorkspaceValues = z.infer<typeof createWorkspaceSchema>;

export function useWorkspaceHome() {
  const rootStore = useStore();
  const { data: currentUser } = useUser();
  const workspaceMutations = React.useMemo(() => new WorkspaceMutations(rootStore), [rootStore]);

  const [isWorkspacesLoading, setIsWorkspacesLoading] = React.useState(true);
  const [isCreating, setIsCreating] = React.useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = React.useState(false);
  const [isJoinModalOpen, setIsJoinModalOpen] = React.useState(false);

  const refetch = React.useCallback(() => {
    return workspaceMutations.fetchList({ direction: "Ascending" });
  }, [workspaceMutations]);

  const userReady = !!currentUser?.id && rootStore.currentUserId === currentUser.id;

  const [prevUserReady, setPrevUserReady] = React.useState(userReady);
  if (userReady !== prevUserReady) {
    setPrevUserReady(userReady);
    if (userReady) setIsWorkspacesLoading(true);
  }

  React.useEffect(() => {
    if (!userReady) return;
    refetch()
      .catch((err) => console.error("Failed to fetch workspaces", err))
      .finally(() => setIsWorkspacesLoading(false));
  }, [refetch, userReady]);

  React.useEffect(() => {
    const onJoined = () => {
      refetch().catch((err) => console.error("Failed to refresh workspaces", err));
    };
    signalRService.on("WorkspaceJoined", onJoined);
    return () => signalRService.off("WorkspaceJoined", onJoined);
  }, [refetch]);

  const workspaces = rootStore.workspaceStore.all;

  const handleCreateWorkspace = React.useCallback(
    async (data: Omit<CreateWorkspaceValues, "theme">) => {
      setIsCreating(true);
      try {
        await workspaceMutations.create({ ...data, theme: "Dark" });
        toast.success("Workspace created successfully");
        setIsCreateModalOpen(false);
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to create workspace");
      } finally {
        setIsCreating(false);
      }
    },
    [workspaceMutations],
  );

  const handlePinWorkspace = React.useCallback(
    (workspaceId: string, isPinned: boolean) => {
      workspaceMutations.pin(workspaceId, isPinned).catch((error: unknown) => {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to update pin");
      });
    },
    [workspaceMutations],
  );

  const handleRemoveWorkspace = React.useCallback(
    (workspaceId: string) => {
      workspaceMutations.removeFromList(workspaceId).catch((error: unknown) => {
        console.error("Failed to remove workspace from list", error);
      });
    },
    [workspaceMutations],
  );

  return {
    workspaces,
    isWorkspacesLoading,
    isCreating,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isJoinModalOpen,
    setIsJoinModalOpen,
    handleCreateWorkspace,
    handlePinWorkspace,
    handleRemoveWorkspace,
  };
}

export function useJoinWorkspaceByCode() {
  const rootStore = useStore();
  const workspaceMutations = React.useMemo(() => new WorkspaceMutations(rootStore), [rootStore]);

  return {
    mutate: async (joinCode: string) => {
      try {
        const data = await workspaceMutations.joinByCode(joinCode);
        if (data.membershipStatus === "Pending") {
          toast.info("Join request sent. Waiting for approval.");
        } else {
          toast.success("Joined workspace successfully");
          await workspaceMutations.fetchList({ direction: "Ascending" });
        }
        return data;
      } catch (error: unknown) {
        const err = error as { message?: string; data?: { Description?: string } };
        toast.error(err.data?.Description || err.message || "Failed to join workspace");
        throw error;
      }
    },
  };
}
