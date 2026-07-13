import { forwardRef, useImperativeHandle, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { SpaceMutations } from "@/mutations/space.mutations";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { DeleteConfirmationDialog } from "@/features/workspace/contents/hierarchy/hierarchy-components/context-menus/shared";
import { SpaceSettingsDialog } from "./space-settings-dialog";
import {
  DEFAULT_SPACE_RAIL_TAB_ORDER,
  normalizeTabOrder,
  type SpaceRailTabKey,
} from "./space-rail-tabs";

export interface SpaceSettingsFlowHandle {
  open: () => void;
}

interface SpaceSettingsFlowProps {
  spaceId: string;
}

export const SpaceSettingsFlow = observer(forwardRef<SpaceSettingsFlowHandle, SpaceSettingsFlowProps>(
  function SpaceSettingsFlow({ spaceId }, ref) {
    const rootStore = useWorkspaceRootStore();
    const syncEngine = useSyncEngine();
    const navigate = useNavigate();
    const { isAdmin: canManage } = useWorkspaceRole();
    const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

    const [isSettingsOpen, setIsSettingsOpen] = useState(false);
    const [isWorkflowOpen, setIsWorkflowOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const [workflowReturnsToSettings, setWorkflowReturnsToSettings] = useState(false);

    useImperativeHandle(ref, () => ({ open: () => setIsSettingsOpen(true) }), []);

    const [rawPinnedTabs, setRawPinnedTabs] = useLocalStorage<Record<string, SpaceRailTabKey>>(
      "space-pinned-tabs",
      {}
    );
    const pinnedTab = rawPinnedTabs[spaceId];
    const handlePinTab = (tab: SpaceRailTabKey | null) => {
      const next = { ...rawPinnedTabs };
      if (tab) next[spaceId] = tab;
      else delete next[spaceId];
      setRawPinnedTabs(next);
    };

    const [rawTabOrder, setRawTabOrder] = useLocalStorage<SpaceRailTabKey[]>(
      "space-rail-tab-order",
      DEFAULT_SPACE_RAIL_TAB_ORDER
    );
    const tabOrder = normalizeTabOrder(rawTabOrder);

    const space = rootStore.spaceStore.getById(spaceId);

    const handleDelete = async () => {
      if (!space) return;
      try {
        await spaceMutations.delete(spaceId);
        navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: space.workspaceId ?? "" } });
      } catch (err) {
        console.error("Failed to delete space", err);
        toast.error(extractErrorMessage(err, "Failed to delete space"));
      }
    };

    if (!space) return null;

    return (
      <>
        <SpaceSettingsDialog
          isOpen={isSettingsOpen}
          onClose={() => setIsSettingsOpen(false)}
          tabOrder={tabOrder}
          onTabOrderChange={setRawTabOrder}
          spaceName={space.name}
          onSpaceNameChange={(name) => {
            spaceMutations.update(spaceId, { name }).catch((err) => {
              console.error("Failed to rename space", err);
              toast.error(extractErrorMessage(err, "Failed to rename space"));
            });
          }}
          spaceIcon={space.icon ?? "LayoutGrid"}
          spaceColor={space.color ?? "#3b82f6"}
          onSpaceIconChange={(icon, color) => {
            spaceMutations.update(spaceId, { icon, color }).catch((err) => {
              console.error("Failed to update space icon", err);
              toast.error(extractErrorMessage(err, "Failed to update space icon"));
            });
          }}
          pinnedTab={pinnedTab ?? null}
          onPinTabChange={handlePinTab}
          onOpenWorkflow={canManage ? () => {
            setIsSettingsOpen(false);
            setWorkflowReturnsToSettings(true);
            setIsWorkflowOpen(true);
          } : undefined}
          onDeleteSpace={canManage ? () => {
            setIsSettingsOpen(false);
            setIsDeleteOpen(true);
          } : undefined}
        />

        <CreateStatusForm
          isOpen={isWorkflowOpen}
          onClose={() => {
            setIsWorkflowOpen(false);
            if (workflowReturnsToSettings) {
              setIsSettingsOpen(true);
              setWorkflowReturnsToSettings(false);
            }
          }}
          spaceId={spaceId}
        />

        <DeleteConfirmationDialog
          open={isDeleteOpen}
          onOpenChange={setIsDeleteOpen}
          title="Delete Space"
          description={`Are you sure you want to delete "${space.name}"? This will delete all folders and tasks inside it and cannot be undone.`}
          onConfirm={handleDelete}
        />
      </>
    );
  }
));
