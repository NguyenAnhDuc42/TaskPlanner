import * as React from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { SpaceBoard } from "./space-components/space-board";
import { SpaceViewRail } from "./space-components/space-view-rail";
import { SpaceActivity } from "./space-components/space-activity";
import { SpaceFilterBar } from "./space-components/space-filter-bar";
import type { SpaceBoardFilter, SpaceBoardSortBy } from "./space-board-types";
import { SpaceSettingsFlow, type SpaceSettingsFlowHandle } from "./space-components/space-settings-flow";
import {
  DEFAULT_SPACE_RAIL_TAB_ORDER,
  isSpaceRailTabKey,
  normalizeTabOrder,
  type SpaceRailTabKey,
} from "./space-components/space-rail-tabs";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { DynamicIcon } from "@/components/dynamic-icon";
import { FavoriteButton } from "@/components/favorite-button";
import { EntityLayerType } from "@/types/entity-layer-type";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { LoadingScreen } from "@/components/loading-screen";
import { SpaceDetail } from "./space-components/space-detail";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreVertical, Settings, Trash2 } from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";

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
  const syncEngine = useSyncEngine();
  const navigate = useNavigate();
  const { ready, error } = useSyncReady();
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const spaceMutations = React.useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const [hiddenStatusIds, setHiddenStatusIds] = React.useState<string[]>([]);
  const [hideUnclassified, setHideUnclassified] = React.useState(false);
  const [boardFilter, setBoardFilter] = React.useState<SpaceBoardFilter>({});
  const [sortBy, setSortBy] = React.useState<SpaceBoardSortBy>("priority");
  const [searchInput, setSearchInput] = React.useState("");
  const [showSubtasks, setShowSubtasks] = React.useState(false);
  const boardStatuses = rootStore.statusStore.getVisibleForSpace(spaceId);

  const space = rootStore.spaceStore.getById(spaceId);

  const handleDelete = async () => {
    const workspaceId = space?.workspaceId ?? "";
    try {
      await spaceMutations.delete(spaceId);
      navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
    } catch (err) {
      console.error("Failed to delete space", err);
    }
  };

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
          leading={
            <div className="flex items-center gap-1.5 text-xs min-w-0">
              <DynamicIcon
                name={space.icon ?? "Orbit"}
                size={14}
                color={space.color ?? "#ffffff"}
                className="shrink-0"
              />
              <span className="font-semibold text-foreground/80 truncate max-w-48">{space.name}</span>
              <FavoriteButton
                entityId={spaceId}
                entityLayerType={EntityLayerType.ProjectSpace}
                iconSize={13}
                className="opacity-100 shrink-0"
              />
            </div>
          }
          tabsTrailing={
            activeTab === "items" ? (
              <SpaceFilterBar
                statuses={boardStatuses}
                hiddenStatusIds={hiddenStatusIds}
                setHiddenStatusIds={setHiddenStatusIds}
                filter={boardFilter}
                onFilterChange={setBoardFilter}
                searchInput={searchInput}
                onSearchChange={setSearchInput}
                isFullyLoaded={ready}
                hideUnclassified={hideUnclassified}
                onToggleUnclassified={() => setHideUnclassified((v) => !v)}
                onOpenWorkflow={canManage ? () => setIsWorkflowOpen(true) : undefined}
                sortBy={sortBy}
                onSortByChange={setSortBy}
                members={rootStore.memberStore.all}
                showSubtasks={showSubtasks}
                onToggleSubtasks={() => setShowSubtasks((v) => !v)}
              />
            ) : undefined
          }
          trailing={
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  className="flex items-center h-7 px-1.5 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer"
                >
                  <MoreVertical className="h-3.5 w-3.5" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onClick={() => settingsFlowRef.current?.open()}
                  className="cursor-pointer"
                >
                  <Settings className="h-4 w-4 mr-2" />
                  Space Settings
                </DropdownMenuItem>
                {canManage && (
                  <DropdownMenuItem
                    onClick={() => setIsDeleteOpen(true)}
                    className="text-destructive focus:text-destructive focus:bg-destructive/10 cursor-pointer"
                  >
                    <Trash2 className="h-4 w-4 mr-2" />
                    Delete Space
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          }
        />

        <div className="flex-1 overflow-hidden flex flex-col relative">
          {activeTab === "items" && (
            <SpaceBoard
              spaceId={spaceId}
              filter={boardFilter}
              sortBy={sortBy}
              searchInput={searchInput}
              hiddenStatusIds={hiddenStatusIds}
              setHiddenStatusIds={setHiddenStatusIds}
              hideUnclassified={hideUnclassified}
              setHideUnclassified={setHideUnclassified}
              showSubtasks={showSubtasks}
            />
          )}
          {activeTab === "detail" && <SpaceDetail spaceId={spaceId} />}
          {activeTab === "activity" && <SpaceActivity spaceId={spaceId} />}
        </div>
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => setIsWorkflowOpen(false)}
        spaceId={space?.id}
      />

      <SpaceSettingsFlow ref={settingsFlowRef} spaceId={spaceId} />

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Space"
        description={`Are you sure you want to delete "${space.name}"? This will delete all folders and tasks inside it and cannot be undone.`}
        onConfirm={handleDelete}
      />
    </>
  );
});
