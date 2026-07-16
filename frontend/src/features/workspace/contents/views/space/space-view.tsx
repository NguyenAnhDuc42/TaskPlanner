import * as React from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { SpaceBoard } from "./space-components/space-board";
import { SpaceViewRail } from "./space-components/space-view-rail";
import { SpaceCommsPlaceholder } from "./space-components/space-comms-placeholder";
import { SpaceSettingsFlow, type SpaceSettingsFlowHandle } from "./space-components/space-settings-flow";
import {
  DEFAULT_SPACE_RAIL_TAB_ORDER,
  isSpaceRailTabKey,
  normalizeTabOrder,
  type SpaceRailTabKey,
} from "./space-components/space-rail-tabs";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { DynamicIcon } from "@/components/dynamic-icon";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { LoadingScreen } from "@/components/loading-screen";
import { SpaceDetail } from "./space-components/space-detail";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncReady } from "@/sync/sync-provider";

interface SpaceViewBodyProps {
  spaceId: string;
}

export const SpaceViewBody = observer(function SpaceViewBody({ spaceId }: Readonly<SpaceViewBodyProps>) {
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  const settingsFlowRef = React.useRef<SpaceSettingsFlowHandle>(null);

  const [rawPinnedTabs] = useLocalStorage<Record<string, SpaceRailTabKey>>("space-pinned-tabs", {});
  const pinnedTab = rawPinnedTabs[spaceId];

  const resolveInitialTab = React.useCallback((): SpaceRailTabKey => {
    if (pinnedTab && isSpaceRailTabKey(pinnedTab)) return pinnedTab;
    const saved = localStorage.getItem(`global-space-tab`);
    return isSpaceRailTabKey(saved) ? saved : "detail";
  }, [pinnedTab]);

  const [activeTab, setActiveTab] = React.useState<SpaceRailTabKey>(resolveInitialTab);

  const handleTabChange = (tab: SpaceRailTabKey) => {
    setActiveTab(tab);
    localStorage.setItem(`global-space-tab`, tab);
  };
  const [rawTabOrder] = useLocalStorage<SpaceRailTabKey[]>(
    "space-rail-tab-order",
    DEFAULT_SPACE_RAIL_TAB_ORDER
  );
  const tabOrder = normalizeTabOrder(rawTabOrder);
  const { isAdmin: canManage } = useWorkspaceRole();
  const rootStore = useWorkspaceRootStore();
  const { ready, error } = useSyncReady();

  const space = rootStore.spaceStore.getById(spaceId);

  if (!space) {
    if (error) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Failed to load space</span>
        </div>
      );
    }

    if (ready) {
      return (
        <div className="flex flex-col items-center justify-center h-full text-destructive/80 space-y-2 p-8">
          <DynamicIcon name="AlertTriangle" size={32} />
          <span className="text-sm font-medium">Space Not Found</span>
          <span className="text-xs text-muted-foreground">The space may have been deleted by another user.</span>
        </div>
      );
    }

    return <LoadingScreen />;
  }

  return (
    <>
      <div className="h-full w-full flex flex-col overflow-hidden relative">
        <SpaceViewRail
          orientation="top"
          tabOrder={tabOrder}
          activeTab={activeTab}
          onTabChange={handleTabChange}
          onOpenSettings={() => settingsFlowRef.current?.open()}
        />

        <div className="flex-1 overflow-hidden flex flex-col relative">
          {activeTab === "items" && (
            <SpaceBoard
              spaceId={spaceId}
              onOpenWorkflow={canManage ? () => setIsWorkflowOpen(true) : undefined}
            />
          )}
          {activeTab === "detail" && <SpaceDetail spaceId={spaceId} />}
          {activeTab === "comms" && <SpaceCommsPlaceholder />}
        </div>
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => setIsWorkflowOpen(false)}
        spaceId={space?.id}
      />

      <SpaceSettingsFlow ref={settingsFlowRef} spaceId={spaceId} />
    </>
  );
});
