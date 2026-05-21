import { useEffect, useMemo, useRef } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { workspaceKeys } from "@/features/main/query-keys";
import type { TaskViewData } from "../../layer-detail-types";
import { buildColumns } from "./folder-dnd-helpers";
import { Priority } from "@/types/priority";
import { useItemsStore, selectHasPendingUpdates } from "../../hooks/use-items-store";
import { UnifiedBoardView } from "../unified-board-view";
import { UnifiedListView } from "../unified-list-view";

interface FolderItemsViewProps {
  viewData: TaskViewData;
  folderId: string;
  viewMode: "board" | "list";
}

export function FolderItemsView({
  viewData,
  folderId,
  viewMode,
}: FolderItemsViewProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false }) as unknown as {
    workspaceId: string;
  };

  const columns = useItemsStore((s) => s.columns);
  const isInitialized = useItemsStore((s) => s.isInitialized);
  const initializeColumns = useItemsStore((s) => s.initializeColumns);
  const setLayerInfo = useItemsStore((s) => s.setLayerInfo);
  const moveItem = useItemsStore((s) => s.moveItem);
  const changePriority = useItemsStore((s) => s.changePriority);
  
  const hasPending = useItemsStore(selectHasPendingUpdates);

  // When opening a new folder, notify the store so it expects initialization
  useEffect(() => {
    setLayerInfo("folder", folderId);
  }, [folderId, setLayerInfo]);

  // STRICT SINGLE SOURCE OF TRUTH:
  // We ONLY load viewData into the local UI state ONCE when opening the folder.
  // After that, Zustand holds the state entirely, and we ONLY rollback if the API fails.
  useEffect(() => {
    if (!isInitialized && viewData && !hasPending) {
      initializeColumns(buildColumns(viewData));
    }
  }, [viewData, isInitialized, hasPending, initializeColumns]);

  function handleMove(params: {
    activeId: string;
    targetStatusId: string | undefined;
    targetIndex: number;
    previousItemOrderKey: string | undefined;
    nextItemOrderKey: string | undefined;
  }) {
    moveItem(workspaceId, params);
  }

  function handlePriorityChange(itemId: string, priority: Priority) {
    changePriority(workspaceId, itemId, priority);
  }

  if (viewMode === "board") {
    return (
      <UnifiedBoardView
        columns={columns}
        statuses={viewData.statuses ?? []}
        onMove={handleMove}
        onPriorityChange={handlePriorityChange}
        onTaskClick={(taskId) => {
          navigate({
            to: "/workspaces/$workspaceId/tasks/$taskId",
            params: { workspaceId, taskId },
          });
        }}
        onFolderClick={(folderId) => {
          navigate({
            to: "/workspaces/$workspaceId/folders/$folderId",
            params: { workspaceId, folderId },
          });
        }}
      />
    );
  }

  return (
    <UnifiedListView
      columns={columns}
      statuses={viewData.statuses ?? []}
      onMove={handleMove}
      onPriorityChange={handlePriorityChange}
      onTaskClick={(taskId) => {
        navigate({
          to: "/workspaces/$workspaceId/tasks/$taskId",
          params: { workspaceId, taskId },
        });
      }}
      onFolderClick={(folderId) => {
        navigate({
          to: "/workspaces/$workspaceId/folders/$folderId",
          params: { workspaceId, folderId },
        });
      }}
    />
  );
}
