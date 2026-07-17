import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { UniversalPicker } from "@/components/universal-picker";
import { useDocumentEditorSlot, useDocumentOutline } from "@/features/workspace/context/document-editor-context";
import { DocumentOutlineRail } from "@/components/document-outline-rail";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { SpaceMutations } from "@/mutations/space.mutations";

interface SpaceDocumentsPanelProps {
  spaceId: string;
}

export const SpaceDocumentsPanel = observer(function SpaceDocumentsPanel({ spaceId }: SpaceDocumentsPanelProps) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const space = rootStore.spaceStore.getById(spaceId);
  const { canCreateContent: canEdit, isAdmin: canManage } = useWorkspaceRole();
  const documentSlotRef = useDocumentEditorSlot(space?.defaultDocumentId, canEdit);
  const { outline, navigate: navigateToHeading } = useDocumentOutline(space?.defaultDocumentId);

  if (!space) return null;

  const updateField = (patches: Parameters<SpaceMutations["updateLocal"]>[1]) => {
    spaceMutations.updateLocal(spaceId, patches).catch((err) => console.error("Failed to apply local space update", err));
    scheduleFlush();
  };

  return (
    <div className="relative flex flex-col h-full overflow-hidden">
      <div className="flex-1 overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
        <div className="px-8 pt-8 pb-4">
          <div className="flex items-center gap-3">
            <UniversalPicker
              icon={space.icon || "Orbit"}
              color={space.color || "#ffffff"}
              onSelect={(icon, color) => updateField({ icon, color })}
              size="lg"
            />
            <div className="min-w-0 flex-1">
              <input
                key={spaceId}
                className="text-xl font-black text-foreground/90 tracking-tight leading-none bg-transparent border-none outline-none w-full hover:bg-muted/20 focus:bg-muted/30 px-1 rounded transition-colors"
                defaultValue={space.name}
                readOnly={!canManage}
                onBlur={e => {
                  if (canManage && e.target.value.trim() && e.target.value !== space.name)
                    updateField({ name: e.target.value.trim() });
                }}
                onKeyDown={e => { if (e.key === "Enter") e.currentTarget.blur(); }}
              />
            </div>
          </div>
        </div>

        <div className="mx-8 border-t border-border/15" />

        <div className="px-8 py-4">
          {space.defaultDocumentId ? (
            <div ref={documentSlotRef} />
          ) : (
            <p className="text-xs text-muted-foreground/40 px-1 mt-2">No document for this space yet.</p>
          )}
        </div>
      </div>

      <div className="absolute inset-y-3 right-4 z-10 w-7 pointer-events-none">
        <div className="pointer-events-auto absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
          <DocumentOutlineRail outline={outline} onNavigate={navigateToHeading} />
        </div>
      </div>
    </div>
  );
});
