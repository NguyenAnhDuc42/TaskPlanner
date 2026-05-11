import { useState, useEffect, useCallback, useRef } from "react";
import { FolderHeader } from "./folder-header";
import { LayerTabs } from "../../components/layer-tabs";
import { FolderOverview } from "./folder-overview";

import { FolderListView } from "./folder-list-view";
import { FolderSidebar } from "./folder-sidebar";
import { AttachmentSection } from "../../components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "../../layer-detail-types";
import { cn } from "@/lib/utils";
import { useFolderDetail, useUpdateFolder, useFolderItems } from "./folder-api";
import { useWorkspaceWorkflows } from "@/features/workspace/api";
import { useDebounce } from "@/hooks/use-debounce";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useFolderRealtime } from "./folder-realtime";
import { useTaskRealtime } from "../task/task-realtime";
import { FolderBoardView } from "./folder-board-view";

interface FolderViewProps {
  workspaceId: string;
  folderId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function FolderView({ workspaceId, folderId }: FolderViewProps) {
  const { data: viewData, isLoading, isError } = useFolderDetail(workspaceId, folderId);
  const { data: itemsData, isLoading: itemsLoading } = useFolderItems(folderId);
  useFolderRealtime(workspaceId);
  useTaskRealtime(workspaceId);
  useWorkspaceWorkflows(workspaceId);
  const [activeTab, setActiveTab] = useState<MainViewTab>(() => (localStorage.getItem("folderTab") as MainViewTab) || "overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>(() => (localStorage.getItem("folderViewMode") as ItemsViewMode) || "list");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");

  useEffect(() => { localStorage.setItem("folderTab", activeTab); }, [activeTab]);
  useEffect(() => { localStorage.setItem("folderViewMode", viewMode); }, [viewMode]);

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

  const updateFolder = useUpdateFolder(onMutationSettled);
  const isSaving = updateFolder.isPending;

  const performUpdate = useCallback(
    (updates: any, targetDraft: any) => {
      if (!viewData?.id) return;
      isSavingRef.current = true;
      lastSentDraft.current = targetDraft;
      updateFolder.mutate({ folderId: viewData.id, ...updates });
    },
    [viewData?.id, updateFolder]
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
    if (debouncedDraft.statusId !== st.statusId) updates.statusId = debouncedDraft.statusId;
    if (debouncedDraft.isPrivate !== st.isPrivate) updates.isPrivate = debouncedDraft.isPrivate;
    if (debouncedDraft.startDate !== st.startDate) updates.startDate = debouncedDraft.startDate;
    if (debouncedDraft.dueDate !== st.dueDate) updates.dueDate = debouncedDraft.dueDate;

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

  if (isLoading && !viewData) return <div>Loading...</div>;
  if (isError) return <div>Failed to load folder</div>;
  if (!viewData) return null;

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <FolderHeader
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
        layerType={EntityLayerType.ProjectFolder}
      />

      <div className="flex-1 flex relative">
        <div className="flex-1 relative min-w-0">
          {activeTab === "overview" && (
            <FolderOverview 
              viewData={viewData} 
              draft={draft} 
              onChange={onDraftChange} 
            />
          )}
          {activeTab === "items" && (
            itemsLoading ? (
              <div>Loading items...</div>
            ) : !itemsData ? (
              <div>No items found</div>
            ) : viewMode === "board" ? (
              <FolderBoardView viewData={itemsData} folderId={folderId} />
            ) : (
              <FolderListView viewData={itemsData} />
            )
          )}
        </div>

        <div
          className={cn(
            "h-full transition-all duration-300 ease-in-out flex items-start overflow-hidden",
            rightPanelType ? "w-[320px] opacity-100" : "w-0 opacity-0",
          )}
        >
          <div className="w-[320px] h-full p-1">
            <div className="w-full h-full rounded-md border border-border/40 bg-muted/30 backdrop-blur-xl shadow-2xl overflow-hidden duration-300">
              <div className="h-full overflow-y-auto no-scrollbar p-2 py-4">
                {rightPanelType === "properties" && (
                  <FolderSidebar viewData={viewData} draft={draft} onChange={onDraftChange} />
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
