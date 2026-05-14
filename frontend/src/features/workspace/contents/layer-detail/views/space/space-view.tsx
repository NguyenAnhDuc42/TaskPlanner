import { useState, useEffect, useCallback, useRef } from "react";
import { SpaceHeader } from "./space-header";
import { LayerTabs } from "../../components/layer-tabs";
import { SpaceOverview } from "./space-overview";
import { SpaceBoardView } from "./space-board-view";
import { SpaceListView } from "./space-list-view";
import { SpaceSidebar } from "./space-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { useSpaceDetail, useUpdateSpace, useSpaceItems } from "./space-api";
import { useWorkspaceWorkflows } from "@/features/workspace/api";
import { useDebounce } from "@/hooks/use-debounce";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useSpaceRealtime } from "./space-realtime";
import { useTaskRealtime } from "../task/task-realtime";
import { useFolderRealtime } from "../folder/folder-realtime";
import { LoadingComponent } from "@/components/loading-component";
import { useNavigate } from "@tanstack/react-router";
import { useDeleteSpace } from "@/features/workspace/contents/hierarchy/hierarchy-api";


interface SpaceViewProps {
  workspaceId: string;
  spaceId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function SpaceView({ workspaceId, spaceId }: SpaceViewProps) {
  const { data: viewData, isError } = useSpaceDetail(workspaceId, spaceId);
  const { data: itemsData, isLoading: itemsLoading } = useSpaceItems(spaceId);
  useSpaceRealtime(workspaceId);
  useFolderRealtime(workspaceId);
  useTaskRealtime(workspaceId);
  useWorkspaceWorkflows(workspaceId);
  const [activeTab, setActiveTab] = useState<MainViewTab>(() => (localStorage.getItem("spaceTab") as MainViewTab) || "overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>(() => (localStorage.getItem("spaceViewMode") as ItemsViewMode) || "list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  useEffect(() => { localStorage.setItem("spaceTab", activeTab); }, [activeTab]);
  useEffect(() => { localStorage.setItem("spaceViewMode", viewMode); }, [viewMode]);

  const [draft, setDraft] = useState<any>(null);
  const [isDirty, setIsDirty] = useState(false);
  const isSavingRef = useRef(false);
  const lastSentDraft = useRef<any>(null);
  const serverTruth = useRef<any>(null);
  const draftRef = useRef<any>(null);

  useEffect(() => {
    if (viewData && !isDirty) {
      setDraft(viewData);
      serverTruth.current = viewData;
      draftRef.current = viewData;
    }
  }, [viewData, isDirty]);

  useEffect(() => {
    draftRef.current = draft;
  }, [draft]);

  const onMutationSettled = useCallback(() => {
    isSavingRef.current = false;
    const currentDraft = draftRef.current;
    if (lastSentDraft.current && JSON.stringify(lastSentDraft.current) === JSON.stringify(currentDraft)) {
      setIsDirty(false);
    }
    serverTruth.current = { ...serverTruth.current, ...lastSentDraft.current };
  }, []);

  const { mutate: mutateSpace, isPending: isSaving } = useUpdateSpace(onMutationSettled);

  const performUpdate = useCallback(
    (updates: any, targetDraft: any) => {
      if (!viewData?.id) return;
      isSavingRef.current = true;
      lastSentDraft.current = targetDraft;
      mutateSpace({ spaceId: viewData.id, ...updates });
    },
    [viewData?.id, mutateSpace]
  );

  const debouncedDraft = useDebounce(draft, 3000);

  useEffect(() => {
    if (!debouncedDraft || !isDirty) return;
    const st = serverTruth.current;
    if (!st) return;

    const updates: any = {};
    if (debouncedDraft.name !== st.name) updates.name = debouncedDraft.name;
    if (debouncedDraft.icon !== st.icon) updates.icon = debouncedDraft.icon;
    if (debouncedDraft.color !== st.color) updates.color = debouncedDraft.color;
    if (debouncedDraft.description !== st.description) updates.description = debouncedDraft.description;
    if (debouncedDraft.isPrivate !== st.isPrivate) updates.isPrivate = debouncedDraft.isPrivate;

    if (Object.keys(updates).length > 0) {
      performUpdate(updates, debouncedDraft);
    }
  }, [debouncedDraft, performUpdate]);

  const onDraftChange = (updates: Partial<any>) => {
    setIsDirty(true);
    setDraft((prev: any) => ({ ...prev, ...updates }));
  };

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  const { mutate: deleteSpace } = useDeleteSpace(workspaceId);
  const navigate = useNavigate();

  const handleDelete = () => {
    if (window.confirm("Are you sure you want to delete this space?")) {
      deleteSpace(spaceId, {
        onSuccess: () => {
          navigate({ to: `/workspaces/${workspaceId}` });
        },
      });
    }
  };

  if (isError) return <div>Failed to load space</div>;
  

  if (!viewData) return null;

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <SpaceHeader
        onDelete={handleDelete}
        viewData={viewData}
        draft={draft}
        isSaving={isSaving}
        isDirty={isDirty}
        activeTab={activeTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        rightPanelType={rightPanelType}
        onToggleRightPanel={toggleRightPanel}
      />

      <LayerTabs
        activeTab={activeTab}
        onTabChange={setActiveTab}
        layerType={EntityLayerType.ProjectSpace}
      />

      <div className="flex-1 flex relative">
        <div className="flex-1 relative min-w-0">
          {activeTab === "overview" && (
            <SpaceOverview 
              viewData={viewData} 
              draft={draft} 
              onChange={onDraftChange} 
              rightPanelType={rightPanelType}
            />
          )}
          {activeTab === "items" && (
            itemsLoading ? (
              <LoadingComponent />
            ) : !itemsData ? (
              <div>No items found</div>
            ) : viewMode === "board" ? (
              <SpaceBoardView viewData={itemsData} spaceId={spaceId} />
            ) : (
              <SpaceListView viewData={itemsData} />
            )
          )}
        </div>

        <div
          className={cn(
            "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
            (rightPanelType && activeTab === "overview") ? "w-[320px] opacity-100" : "w-0 opacity-0",
          )}
        >
          <div className="w-[320px] h-full p-1">
            <div className="w-full h-full rounded-md border border-border/40 bg-background/95 backdrop-blur-md shadow-2xl overflow-hidden duration-300">
              <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                {rightPanelType === "properties" && (
                  <SpaceSidebar viewData={viewData} draft={draft} onChange={onDraftChange} />
                )}
                {rightPanelType === "attachments" && <AttachmentSection />}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
