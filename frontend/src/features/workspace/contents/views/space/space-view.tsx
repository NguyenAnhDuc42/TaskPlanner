import * as React from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { EntityViewFrame } from "../entity-view-frame";
import { SpaceBoard } from "./space-components/space-board";
import { SpaceViewRail } from "./space-components/space-view-rail";
import { SpaceCommsPlaceholder } from "./space-components/space-comms-placeholder";
import { SpacePropertiesPanel } from "./space-components/space-properties-panel";
import { SpaceSettingsDialog } from "./space-components/space-settings-dialog";
import {
  DEFAULT_SPACE_RAIL_TAB_ORDER,
  isSpaceRailTabKey,
  normalizeTabOrder,
  type SpaceRailTabKey,
} from "./space-components/space-rail-tabs";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { MoreVertical, Trash2, PanelRight, X } from "lucide-react";
import { FavoriteButton } from "@/components/favorite-button";
import { DynamicIcon } from "@/components/dynamic-icon";
import { EntityLayerType } from "@/types/entity-layer-type";
import { CreateStatusForm } from "@/features/workspace/components/forms/workflow-management-form";
import { LoadingScreen } from "@/components/loading-screen";

import { useNavigate } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { SpaceDetail } from "./space-components/space-detail";
import { DeleteConfirmationDialog } from "../../hierarchy/hierarchy-components/context-menus/shared";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine, useSyncReady } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";


interface SpaceViewProps {
  spaceId: string;
}

export const SpaceView = observer(function SpaceView({ spaceId }: Readonly<SpaceViewProps>) {
  const [isWorkflowOpen, setIsWorkflowOpen] = React.useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = React.useState(false);
  const [isPropertiesOpen, setIsPropertiesOpen] = React.useState(false);
  // Tracks whether Workflow Manager was launched from inside Settings, so closing it returns to
  // Settings instead of just exiting the whole flow — Workflow is a sub-step of Settings, not a
  // fully separate top-level dialog, when opened that way.
  const [workflowReturnsToSettings, setWorkflowReturnsToSettings] = React.useState(false);

  // Per-space pinned default tab (Settings) takes priority over the last-used-anywhere fallback.
  const [rawPinnedTabs, setRawPinnedTabs] = useLocalStorage<Record<string, SpaceRailTabKey>>(
    "space-pinned-tabs",
    {}
  );
  const pinnedTab = rawPinnedTabs[spaceId];

  const resolveInitialTab = React.useCallback((): SpaceRailTabKey => {
    if (pinnedTab && isSpaceRailTabKey(pinnedTab)) return pinnedTab;
    const saved = localStorage.getItem(`global-space-tab`);
    return isSpaceRailTabKey(saved) ? saved : "detail";
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pinnedTab]);

  const [activeTab, setActiveTab] = React.useState<SpaceRailTabKey>(resolveInitialTab);

  // Re-resolve on space switch — SpaceView doesn't necessarily remount when navigating between
  // spaces (same route pattern, different param), so the initial useState lazy-init alone
  // wouldn't fire again.
  React.useEffect(() => {
    setActiveTab(resolveInitialTab());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [spaceId]);

  const handleTabChange = (tab: SpaceRailTabKey) => {
    setActiveTab(tab);
    localStorage.setItem(`global-space-tab`, tab);
  };
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
  const { isAdmin: canManage } = useWorkspaceRole();
  const navigate = useNavigate();
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const { ready, error } = useSyncReady();
  const spaceMutations = React.useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);

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
    <EntityViewFrame
      topHeader={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-1.5 text-xs min-w-0">
            <DynamicIcon
              name={space?.icon ?? "LayoutGrid"}
              size={14}
              color={space?.color ?? "#3b82f6"}
              className="shrink-0"
            />
            <span className="font-semibold text-foreground/80 truncate">
              {space?.name ?? "Space"}
            </span>
            {space && (
              <FavoriteButton
                entityId={spaceId}
                entityLayerType={EntityLayerType.ProjectSpace}
                iconSize={13}
                className="opacity-100 shrink-0"
              />
            )}
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
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
        </div>
      }
    >
      <div className="h-full w-full flex overflow-hidden relative">
        <SpaceViewRail
          tabOrder={tabOrder}
          activeTab={activeTab}
          onTabChange={handleTabChange}
          onOpenSettings={() => setIsSettingsOpen(true)}
        />

        {/* Content area — top bar (search/filter/columns) lives inside SpaceBoard itself */}
        <div className="flex-1 overflow-hidden flex flex-col relative">
          {activeTab === "items" && (
            <SpaceBoard
              spaceId={spaceId}
              onWorkflowOpen={canManage ? () => {
                setWorkflowReturnsToSettings(false);
                setIsWorkflowOpen(true);
              } : undefined}
            />
          )}
          {activeTab === "detail" && <SpaceDetail spaceId={spaceId} />}
          {activeTab === "comms" && <SpaceCommsPlaceholder />}

          {/* Document-only — task stats/status-breakdown and the changes feed read as "about this
              project" metadata, which belongs next to the plan (Document), not the live working
              surface (Tasks board). Pinned over the content pane itself, not the app-chrome header
              — matches where Linear's issue-view expand button actually lives. */}
          {activeTab === "detail" && !isPropertiesOpen && (
            <button
              type="button"
              onClick={() => setIsPropertiesOpen(true)}
              title="Open properties panel"
              className="absolute top-3 right-3 z-10 h-7 w-7 flex items-center justify-center rounded-md border border-border/30 shadow-sm transition-colors cursor-pointer bg-card/80 backdrop-blur-sm text-muted-foreground hover:text-foreground hover:bg-muted/60"
            >
              <PanelRight className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        {activeTab === "detail" && isPropertiesOpen && (
          <div className="w-64 shrink-0 border-l border-border overflow-hidden flex flex-col">
            <div className="h-9 flex items-center justify-end px-2 border-b border-border/30 shrink-0">
              <button
                type="button"
                onClick={() => setIsPropertiesOpen(false)}
                title="Close properties panel"
                className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>
            <SpacePropertiesPanel spaceId={spaceId} />
          </div>
        )}
      </div>

      <CreateStatusForm
        isOpen={isWorkflowOpen}
        onClose={() => {
          setIsWorkflowOpen(false);
          if (workflowReturnsToSettings) {
            setIsSettingsOpen(true);
            setWorkflowReturnsToSettings(false);
          }
        }}
        spaceId={space?.id}
      />

      <SpaceSettingsDialog
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        tabOrder={tabOrder}
        onTabOrderChange={setRawTabOrder}
        spaceName={space.name}
        onSpaceNameChange={(name) => {
          spaceMutations.update(spaceId, { name }).catch((err) => console.error("Failed to rename space", err));
        }}
        spaceIcon={space.icon ?? "LayoutGrid"}
        spaceColor={space.color ?? "#3b82f6"}
        onSpaceIconChange={(icon, color) => {
          spaceMutations.update(spaceId, { icon, color }).catch((err) => console.error("Failed to update space icon", err));
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

      <DeleteConfirmationDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Delete Space"
        description={`Are you sure you want to delete "${space?.name}"? This will delete all folders and tasks inside it and cannot be undone.`}
        onConfirm={handleDelete}
      />
    </EntityViewFrame>
  );
});
