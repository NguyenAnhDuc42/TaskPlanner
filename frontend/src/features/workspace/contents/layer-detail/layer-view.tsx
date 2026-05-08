import { useState, useEffect, useCallback, useRef } from "react";
import { EntityLayerType } from "@/types/entity-layer-type";
import { LayerHeader } from "./components/layer-header";
import { LayerTabs } from "./components/layer-tabs";
import { OverviewView } from "./views/overview-view";
import { TaskDetailView } from "./views/task-detail-view";
import { ItemsView } from "./views/items-view";
import { PropertySidebar } from "./components/overview/property-sidebar";
import { AttachmentSection } from "./components/overview/attachment-section";
import type { MainViewTab, ItemsViewMode } from "./layer-detail-types";
import { cn } from "@/lib/utils";
import { useUpdateSpace, useUpdateFolder, useUpdateTask } from "./layer-api";
import { useDebounce } from "@/hooks/use-debounce";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import { hierarchyKeys } from "../hierarchy/hierarchy-keys";

interface LayerViewProps {
  layerType: EntityLayerType;
  viewData: any;
  isLoading: boolean;
  workspaceId: string;
}

export type RightPanelType = "properties" | "attachments" | null;

export function LayerView({ viewData, isLoading, layerType, workspaceId }: LayerViewProps) {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<MainViewTab>("overview");
  const [viewMode, setViewMode] = useState<ItemsViewMode>("board");
  const [rightPanelType, setRightPanelType] = useState<RightPanelType>("properties");


  // 1. Unified Draft State
  const [draft, setDraft] = useState({
    name: viewData?.name || "",
    icon: viewData?.icon || "LayoutGrid",
    color: viewData?.color || "#94a3b8",
    description: viewData?.description || "",
    statusId: viewData?.statusId || null,
    priority: viewData?.priority || 0,
    isPrivate: viewData?.isPrivate || false,
    startDate: viewData?.startDate || null,
    dueDate: viewData?.dueDate || null
  });

  const draftRef = useRef(draft);
  useEffect(() => { draftRef.current = draft; }, [draft]);

  const isDirty = useRef(false);
  const isSavingRef = useRef(false);
  const lastViewDataId = useRef(viewData?.id);
  const serverTruth = useRef(viewData);
  const lastSentDraft = useRef<typeof draft | null>(null);

  // 2. Sync Draft from external changes
  useEffect(() => {
    if (viewData?.id !== lastViewDataId.current) {
      setDraft({
        name: viewData?.name || "",
        icon: viewData?.icon || "LayoutGrid",
        color: viewData?.color || "#94a3b8",
        description: viewData?.description || "",
        statusId: viewData?.statusId || null,
        priority: viewData?.priority || 0,
        isPrivate: viewData?.isPrivate || false,
        startDate: viewData?.startDate || null,
        dueDate: viewData?.dueDate || null
      });
      lastViewDataId.current = viewData?.id;
      serverTruth.current = viewData;
      isDirty.current = false;
      return;
    }

    if (!isDirty.current && !isSavingRef.current) {
      setDraft(prev => ({
        ...prev,
        name: viewData?.name || prev.name,
        icon: viewData?.icon || prev.icon,
        color: viewData?.color || prev.color,
        description: viewData?.description || prev.description,
        statusId: viewData?.statusId || prev.statusId,
        priority: viewData?.priority ?? prev.priority,
        isPrivate: viewData?.isPrivate ?? prev.isPrivate,
        startDate: viewData?.startDate || prev.startDate,
        dueDate: viewData?.dueDate || prev.dueDate
      }));
      serverTruth.current = viewData;
    }
  }, [viewData]);

  // 3. Mutations
  const onMutationSettled = useCallback(() => {
    isSavingRef.current = false;
    const currentDraft = draftRef.current;
    if (lastSentDraft.current && JSON.stringify(lastSentDraft.current) === JSON.stringify(currentDraft)) {
      isDirty.current = false;
    }
    serverTruth.current = { ...serverTruth.current, ...lastSentDraft.current };
  }, []);

  const updateSpace = useUpdateSpace(onMutationSettled);
  const updateFolder = useUpdateFolder(onMutationSettled);
  const updateTask = useUpdateTask(onMutationSettled);

  const isSaving = updateSpace.isPending || updateFolder.isPending || updateTask.isPending;
  useEffect(() => { isSavingRef.current = isSaving; }, [isSaving]);

  const { mutate: mutateSpace } = updateSpace;
  const { mutate: mutateFolder } = updateFolder;
  const { mutate: mutateTask } = updateTask;

  const performUpdate = useCallback(
    (updates: any, targetDraft: typeof draft) => {
      if (!viewData?.id) return;
      isSavingRef.current = true;
      lastSentDraft.current = targetDraft;
      
      if (layerType === EntityLayerType.ProjectSpace)
        mutateSpace({ spaceId: viewData.id, ...updates });
      else if (layerType === EntityLayerType.ProjectFolder)
        mutateFolder({ folderId: viewData.id, ...updates });
      else if (layerType === EntityLayerType.ProjectTask)
        mutateTask({ taskId: viewData.id, ...updates });
    },
    [layerType, viewData?.id, mutateSpace, mutateFolder, mutateTask]
  );

  // 4. IMMEDIATE Sync for UI (Debounced to prevent INP spikes)
  const fastDebouncedDraft = useDebounce(draft, 300);

  useEffect(() => {
    if (!viewData || !isDirty.current) return;

    const keyType = layerType === EntityLayerType.ProjectSpace ? "space" : 
                    layerType === EntityLayerType.ProjectFolder ? "folder" : "task";
    
    queryClient.setQueryData([...workspaceKeys.all, keyType, viewData.id], (old: any) => ({
      ...old,
      ...fastDebouncedDraft
    }));

    const hUpdates = { 
      name: fastDebouncedDraft.name, 
      icon: fastDebouncedDraft.icon, 
      color: fastDebouncedDraft.color, 
      statusId: fastDebouncedDraft.statusId 
    };

    // Update Root Hierarchy (Sidebar - Only for Spaces)
    if (layerType === EntityLayerType.ProjectSpace) {
      queryClient.setQueryData(hierarchyKeys.detail(workspaceId), (old: any) => {
        if (!old?.spaces) return old;
        return { 
          ...old, 
          spaces: old.spaces.map((s: any) => s.id === viewData.id ? { ...s, ...hUpdates } : s) 
        };
      });
    }

    // Update expanded node lists (Folders/Tasks)
    queryClient.setQueriesData({ queryKey: hierarchyKeys.nodeBase(workspaceId) }, (old: any) => {
      if (!old) return old;
      if (old.pages) {
        return {
          ...old,
          pages: old.pages.map((page: any) => ({
            ...page,
            items: page.items?.map((item: any) => item.id === viewData.id ? { ...item, ...hUpdates } : item)
          }))
        };
      }
      if (Array.isArray(old)) {
        return old.map((item: any) => item.id === viewData.id ? { ...item, ...hUpdates } : item);
      }
      return old;
    });
  }, [fastDebouncedDraft, viewData?.id, layerType, workspaceId, queryClient]);

  // 5. DEBOUNCED Save (Server Update)
  const debouncedDraft = useDebounce(draft, 4000);

  useEffect(() => {
    if (!viewData || !isDirty.current || isSavingRef.current) return;

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

  // 6. Save on unmount if dirty
  useEffect(() => {
    return () => {
      if (isDirty.current && !isSavingRef.current) {
        const st = serverTruth.current;
        const d = draftRef.current;
        if (!st) return;

        const updates: any = {};
        if (d.name !== st.name) updates.name = d.name;
        if (d.icon !== st.icon) updates.icon = d.icon;
        if (d.color !== st.color) updates.color = d.color;
        if (d.description !== st.description) updates.description = d.description;
        if (d.statusId !== st.statusId) updates.statusId = d.statusId;
        if (d.isPrivate !== st.isPrivate) updates.isPrivate = d.isPrivate;
        if (d.startDate !== st.startDate) updates.startDate = d.startDate;
        if (d.dueDate !== st.dueDate) updates.dueDate = d.dueDate;

        if (Object.keys(updates).length > 0) {
          performUpdate(updates, d);
        }
      }
    };
  }, [performUpdate]);

  const onDraftChange = (updates: Partial<typeof draft>) => {
    isDirty.current = true;
    setDraft(prev => ({ ...prev, ...updates }));
  };

  const toggleRightPanel = (type: RightPanelType) => {
    setRightPanelType((prev) => (prev === type ? null : type));
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background overflow-hidden relative">
      <LayerHeader
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
        layerType={layerType}
      />

      <div className="flex-1 flex overflow-hidden relative">
        <div className="flex-1 overflow-hidden relative">
          {activeTab === "overview" && (
            layerType === EntityLayerType.ProjectTask ? (
              <TaskDetailView 
                viewData={viewData} 
                draft={draft} 
                onChange={onDraftChange} 
              />
            ) : (
              <OverviewView 
                viewData={viewData} 
                draft={draft} 
                onChange={onDraftChange} 
              />
            )
          )}
          {activeTab === "items" && (
            <ItemsView
              viewData={viewData}
              isLoading={isLoading}
              layerType={layerType}
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
                  <PropertySidebar 
                    layerType={layerType} 
                    viewData={viewData} 
                    draft={draft}
                    onChange={onDraftChange}
                  />
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
