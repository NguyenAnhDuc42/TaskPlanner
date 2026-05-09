import { useState, useEffect, useCallback, useRef } from "react";
import { SpaceHeader } from "./space-header";
import { LayerTabs } from "../../components/layer-tabs";
import { SpaceOverview } from "./space-overview";
import { ItemsView } from "../items-view";
import { SpaceSidebar } from "./space-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { useSpaceDetail, useUpdateSpace } from "./space-api";
import { useDebounce } from "@/hooks/use-debounce";
import { EntityLayerType } from "@/types/entity-layer-type";

interface SpaceViewProps {
  workspaceId: string;
  spaceId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function SpaceView({ workspaceId, spaceId }: SpaceViewProps) {
  const { data: viewData, isLoading, isError } = useSpaceDetail(workspaceId, spaceId);
  const [activeTab, setActiveTab] = useState<MainViewTab>("overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  const [draft, setDraft] = useState<any>(null);
  const isDirty = useRef(false);
  const isSavingRef = useRef(false);
  const lastSentDraft = useRef<any>(null);
  const serverTruth = useRef<any>(null);
  const draftRef = useRef<any>(null);

  useEffect(() => {
    if (viewData) {
      setDraft(viewData);
      serverTruth.current = viewData;
      draftRef.current = viewData;
    }
  }, [viewData]);

  useEffect(() => {
    draftRef.current = draft;
  }, [draft]);

  const onMutationSettled = useCallback(() => {
    isSavingRef.current = false;
    const currentDraft = draftRef.current;
    if (lastSentDraft.current && JSON.stringify(lastSentDraft.current) === JSON.stringify(currentDraft)) {
      isDirty.current = false;
    }
    serverTruth.current = { ...serverTruth.current, ...lastSentDraft.current };
  }, []);

  const updateSpace = useUpdateSpace(onMutationSettled);
  const isSaving = updateSpace.isPending;

  const performUpdate = useCallback(
    (updates: any, targetDraft: any) => {
      if (!viewData?.id) return;
      isSavingRef.current = true;
      lastSentDraft.current = targetDraft;
      updateSpace.mutate({ spaceId: viewData.id, ...updates });
    },
    [viewData?.id, updateSpace]
  );

  const debouncedDraft = useDebounce(draft, 300);

  useEffect(() => {
    if (!debouncedDraft || !isDirty.current) return;
    const st = serverTruth.current;
    if (!st) return;

    const updates: any = {};
    if (debouncedDraft.name !== st.name) updates.name = debouncedDraft.name;
    if (debouncedDraft.icon !== st.icon) updates.icon = debouncedDraft.icon;
    if (debouncedDraft.color !== st.color) updates.color = debouncedDraft.color;
    if (debouncedDraft.description !== st.description) updates.description = debouncedDraft.description;
    if (debouncedDraft.statusId !== st.statusId) updates.statusId = debouncedDraft.statusId;
    if (debouncedDraft.isPrivate !== st.isPrivate) updates.isPrivate = debouncedDraft.isPrivate;
    if (debouncedDraft.startDate !== st.startDate) updates.startDate = debouncedDraft.startDate;
    if (debouncedDraft.dueDate !== st.dueDate) updates.dueDate = debouncedDraft.dueDate;

    if (Object.keys(updates).length > 0) {
      performUpdate(updates, debouncedDraft);
    }
  }, [debouncedDraft, performUpdate]);

  const onDraftChange = (updates: Partial<any>) => {
    isDirty.current = true;
    setDraft((prev: any) => ({ ...prev, ...updates }));
  };

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  if (isLoading && !viewData) return <div>Loading...</div>;
  if (isError) return <div>Failed to load space</div>;
  if (!viewData) return null;

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <SpaceHeader
        viewData={viewData}
        draft={draft}
        isSaving={isSaving}
        isDirty={isDirty.current}
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

      <div className="flex-1 flex overflow-hidden relative">
        <div className="flex-1 overflow-hidden relative">
          {activeTab === "overview" && (
            <SpaceOverview 
              viewData={viewData} 
              draft={draft} 
              onChange={onDraftChange} 
            />
          )}
          {activeTab === "items" && (
            <ItemsView
              viewData={viewData}
              isLoading={isLoading}
              layerType={EntityLayerType.ProjectSpace}
              viewMode={viewMode}
            />
          )}
        </div>

        <div
          className={cn(
            "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
            rightPanelType ? "w-[320px] opacity-100" : "w-0 opacity-0",
          )}
        >
          <div className="w-[320px] h-full p-1">
            <div className="w-full h-full rounded-md border border-border/40 bg-muted/30 backdrop-blur-xl shadow-2xl overflow-hidden animate-in slide-in-from-right-4 duration-300">
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
