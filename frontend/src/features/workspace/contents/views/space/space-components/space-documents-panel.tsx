import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { useDocumentEditorSlot, useDocumentOutline } from "@/features/workspace/context/document-editor-context";
import { DocumentOutlineRail } from "@/components/document-outline-rail";
import { UniversalPicker } from "@/components/universal-picker";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { DocumentMutations } from "@/mutations/document.mutations";
import { DocumentNodeList } from "@/features/workspace/contents/hierarchy/items/document-node-list";
import { useLocalStorage } from "@/hooks/use-local-storage";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";

interface SpaceDocumentsPanelProps {
  spaceId: string;
}

export const SpaceDocumentsPanel = observer(function SpaceDocumentsPanel({ spaceId }: SpaceDocumentsPanelProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const documentMutations = useMemo(() => new DocumentMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { canCreateContent: canEdit } = useWorkspaceRole();

  const space = rootStore.spaceStore.getById(spaceId);
  const roots = rootStore.documentStore.getRootsBySpace(spaceId);

  const [pinnedActiveId, setPinnedActiveId] = useLocalStorage<string | null>(`doc-active:${spaceId}`, null);
  const activeDocumentId =
    pinnedActiveId && rootStore.documentStore.getById(pinnedActiveId)?.spaceId === spaceId
      ? pinnedActiveId
      : roots[0]?.id;
  const activeDocument = activeDocumentId ? rootStore.documentStore.getById(activeDocumentId) : undefined;

  const documentSlotRef = useDocumentEditorSlot(activeDocumentId, canEdit);
  const { outline, navigate: navigateToHeading } = useDocumentOutline(activeDocumentId);

  if (!space) return null;

  const commitTitleChange = (name: string) => {
    if (!activeDocumentId || !name.trim() || name === activeDocument?.name) return;
    documentMutations
      .update(activeDocumentId, { name: name.trim() })
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to rename document")));
  };

  const commitIconChange = (icon: string, color: string) => {
    if (!activeDocumentId) return;
    documentMutations
      .update(activeDocumentId, { icon, color })
      .catch((err) => toast.error(extractErrorMessage(err, "Failed to update document icon")));
  };

  return (
    <div className="relative flex h-full overflow-hidden">
      <div className="w-52 shrink-0 border-r border-border/15 flex flex-col overflow-hidden">
        <div className="px-3 pt-3 pb-1.5 shrink-0">
          <p className="px-0.5 text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Pages</p>
        </div>
        <div className="flex-1 overflow-y-auto px-1.5 pb-3 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
          <DocumentNodeList spaceId={spaceId} activeDocumentId={activeDocumentId ?? ""} onSelect={setPinnedActiveId} />
        </div>
      </div>

      <div className="flex-1 min-w-0 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
        {activeDocumentId && activeDocument ? (
          <>
            <div className="px-8 pt-8 pb-4">
              <div className="flex items-center gap-3">
                <UniversalPicker
                  icon={activeDocument.icon || "FileText"}
                  color={activeDocument.color || "#ffffff"}
                  onSelect={commitIconChange}
                  size="lg"
                />
                <div className="min-w-0 flex-1">
                  <input
                    key={activeDocumentId}
                    className="text-xl font-black text-foreground/90 tracking-tight leading-none bg-transparent border-none outline-none w-full hover:bg-muted/20 focus:bg-muted/30 px-1 rounded transition-colors"
                    defaultValue={activeDocument.name}
                    readOnly={!canEdit}
                    onBlur={(e) => commitTitleChange(e.target.value)}
                    onKeyDown={(e) => { if (e.key === "Enter") e.currentTarget.blur(); }}
                  />
                </div>
              </div>
            </div>

            <div className="mx-8 border-t border-border/15" />

            <div className="px-8 py-4">
              <div ref={documentSlotRef} />
            </div>
          </>
        ) : (
          <p className="text-xs text-muted-foreground/40 px-8 pt-8">No pages yet — add one from the panel on the left.</p>
        )}
      </div>

      <div className="absolute inset-y-3 right-4 z-10 w-7 pointer-events-none">
        <div className="pointer-events-auto absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
          <DocumentOutlineRail outline={outline} onNavigate={navigateToHeading} />
        </div>
      </div>
    </div>
  );
});
