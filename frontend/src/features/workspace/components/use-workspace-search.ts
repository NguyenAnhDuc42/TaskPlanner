import { useDeferredValue, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { EntityLayerType } from "@/types/entity-layer-type";
import { broadcastLocalStorageChange } from "@/hooks/use-local-storage";

export type WorkspaceSearchResult = {
  id: string;
  name: string;
  icon?: string;
  color?: string;
  type: EntityLayerType;
  // Documents need their owning space to navigate there and select the right page.
  spaceId?: string;
};

const MAX_RESULTS_PER_SECTION = 5;

// Shared between the sidebar's inline GlobalSearch and the Cmd+K command palette, so the two
// can't drift into searching different entity sets the way the sidebar previously searched
// Spaces/Tasks only, with Documents never included at all.
export function useWorkspaceSearch(query: string) {
  const rootStore = useWorkspaceRootStore();
  const { workspaceId } = useWorkspace();
  const navigate = useNavigate();
  const deferredQuery = useDeferredValue(query);

  const sections = useMemo((): { label: string; results: WorkspaceSearchResult[] }[] => {
    const q = deferredQuery.trim().toLowerCase();
    if (!q) return [];

    const spaces: WorkspaceSearchResult[] = rootStore.spaceStore.all
      .filter((s) => s.name.toLowerCase().includes(q))
      .map((s) => ({ id: s.id, name: s.name, icon: s.icon, color: s.color, type: EntityLayerType.ProjectSpace }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    const tasks: WorkspaceSearchResult[] = rootStore.taskStore.all
      .filter((t) => t.name.toLowerCase().includes(q))
      .map((t) => ({ id: t.id, name: t.name, icon: t.icon, color: t.color, type: EntityLayerType.ProjectTask }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    const documents: WorkspaceSearchResult[] = rootStore.documentStore.all
      .filter((d) => d.name.toLowerCase().includes(q))
      .map((d) => ({
        id: d.id,
        name: d.name,
        icon: d.icon,
        color: d.color,
        type: EntityLayerType.ProjectDocument,
        spaceId: d.spaceId,
      }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    return [
      { label: "Spaces", results: spaces },
      { label: "Tasks", results: tasks },
      { label: "Documents", results: documents },
    ].filter((section) => section.results.length > 0);
  }, [deferredQuery, rootStore.spaceStore.all, rootStore.taskStore.all, rootStore.documentStore.all]);

  const navigateToResult = (result: WorkspaceSearchResult) => {
    if (!workspaceId) return;
    if (result.type === EntityLayerType.ProjectSpace) {
      navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: result.id } });
    } else if (result.type === EntityLayerType.ProjectTask) {
      navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: result.id } });
    } else if (result.type === EntityLayerType.ProjectDocument && result.spaceId) {
      // Tab/active-document state is localStorage-driven, not URL-driven — set both before
      // navigating so the Space view lands directly on this document's Documents tab.
      localStorage.setItem("global-space-tab", "detail");
      localStorage.setItem(`doc-active:${result.spaceId}`, JSON.stringify(result.id));
      broadcastLocalStorageChange(`doc-active:${result.spaceId}`);
      navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: result.spaceId } });
    }
  };

  return { sections, hasResults: sections.length > 0, navigateToResult };
}
