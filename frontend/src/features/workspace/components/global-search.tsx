import { useDeferredValue, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "@tanstack/react-router";
import { Search } from "lucide-react";
import { DynamicIcon } from "@/components/dynamic-icon";
import { useStore } from "@/stores/root.store";
import { useWorkspace } from "@/features/workspace/context/workspace-context";
import { EntityLayerType } from "@/types/entity-layer-type";

type SearchResult = {
  id: string;
  name: string;
  icon?: string;
  color?: string;
  type: EntityLayerType;
};

const MAX_RESULTS_PER_SECTION = 5;

// Lives in the workspace header, not the hierarchy sidebar — one real search box instead of two
// near-identical ones (the sidebar used to have its own filter input, and this bar was a permanent
// "Elasticsearch Coming Soon" stub). Matches Space/Folder/Task names against data already hydrated
// locally (Bootstrap + Delta), no server round-trip. Picking a result navigates straight to it.
export const GlobalSearch = observer(function GlobalSearch() {
  const { workspaceId } = useWorkspace();
  const rootStore = useStore();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [isFocused, setIsFocused] = useState(false);
  const deferredQuery = useDeferredValue(query);

  const sections = useMemo((): { label: string; results: SearchResult[] }[] => {
    const q = deferredQuery.trim().toLowerCase();
    if (!q) return [];

    const spaces: SearchResult[] = rootStore.spaceStore.all
      .filter((s) => s.name.toLowerCase().includes(q))
      .map((s) => ({ id: s.id, name: s.name, icon: s.icon, color: s.color, type: EntityLayerType.ProjectSpace }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    const folders: SearchResult[] = rootStore.folderStore.all
      .filter((f) => f.name.toLowerCase().includes(q))
      .map((f) => ({ id: f.id, name: f.name, icon: f.icon, color: f.color, type: EntityLayerType.ProjectFolder }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    const tasks: SearchResult[] = rootStore.taskStore.all
      .filter((t) => t.name.toLowerCase().includes(q))
      .map((t) => ({ id: t.id, name: t.name, icon: t.icon, color: t.color, type: EntityLayerType.ProjectTask }))
      .slice(0, MAX_RESULTS_PER_SECTION);

    return [
      { label: "Spaces", results: spaces },
      { label: "Folders", results: folders },
      { label: "Tasks", results: tasks },
    ].filter((section) => section.results.length > 0);
  }, [deferredQuery, rootStore.spaceStore.all, rootStore.folderStore.all, rootStore.taskStore.all]);

  const hasResults = sections.length > 0;

  const handleSelect = (result: SearchResult) => {
    if (!workspaceId) return;
    if (result.type === EntityLayerType.ProjectSpace) {
      navigate({ to: "/workspaces/$workspaceId/spaces/$spaceId", params: { workspaceId, spaceId: result.id } });
    } else if (result.type === EntityLayerType.ProjectFolder) {
      navigate({ to: "/workspaces/$workspaceId/folders/$folderId", params: { workspaceId, folderId: result.id } });
    } else if (result.type === EntityLayerType.ProjectTask) {
      navigate({ to: "/workspaces/$workspaceId/tasks/$taskId", params: { workspaceId, taskId: result.id } });
    }
    setQuery("");
    setIsFocused(false);
  };

  const defaultIcon = (type: EntityLayerType) =>
    type === EntityLayerType.ProjectTask ? "CheckSquare" : type === EntityLayerType.ProjectFolder ? "Folder" : "LayoutGrid";

  return (
    <div className="relative w-full max-w-md rounded-md group">
      <span className="absolute inset-y-0 left-0 flex items-center pl-2.5 pointer-events-none z-10">
        <Search className="h-3.5 w-3.5 text-muted-foreground/60" />
      </span>
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setTimeout(() => setIsFocused(false), 150)}
        placeholder="Search spaces, folders, tasks... (⌘K)"
        className="w-full h-7 bg-secondary/60 hover:bg-secondary/80 focus:bg-secondary/80 border border-transparent focus:border-primary/30 focus:ring-1 focus:ring-primary/20 rounded-md pl-8 pr-3 text-[11px] font-medium placeholder:text-muted-foreground/50 transition-all outline-none shadow-inner relative z-10"
      />

      {isFocused && query && (
        <div className="absolute top-[calc(100%+4px)] left-0 w-full bg-popover border border-border shadow-md rounded-md p-1.5 z-50 max-h-96 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
          {!hasResults ? (
            <div className="flex flex-col items-center justify-center text-center py-5 text-muted-foreground">
              <Search className="h-5 w-5 mb-1.5 opacity-20" />
              <p className="text-xs font-semibold text-foreground/70">No matches</p>
            </div>
          ) : (
            sections.map((section, i) => (
              <div key={section.label} className={i > 0 ? "mt-1.5 pt-1.5 border-t border-border/40" : undefined}>
                <p className="px-2 pb-1 text-[9px] font-bold uppercase tracking-wider text-muted-foreground/50">
                  {section.label}
                </p>
                {section.results.map((result) => (
                  <button
                    key={`${result.type}-${result.id}`}
                    type="button"
                    onMouseDown={(e) => e.preventDefault()}
                    onClick={() => handleSelect(result)}
                    className="w-full flex items-center gap-2 px-2 py-1.5 rounded text-left hover:bg-muted transition-colors"
                  >
                    <DynamicIcon
                      name={result.icon ?? defaultIcon(result.type)}
                      size={14}
                      color={result.color || undefined}
                      className="shrink-0"
                    />
                    <span className="text-[12px] font-medium truncate">{result.name}</span>
                  </button>
                ))}
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
});
