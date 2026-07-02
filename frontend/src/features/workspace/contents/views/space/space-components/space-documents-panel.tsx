import { useMemo } from "react";
import { observer } from "mobx-react-lite";
import { useWorkspaceRole } from "@/features/workspace/context/use-workspace-role";
import { UniversalPicker } from "@/components/universal-picker";
import { BlockEditor } from "@/components/blockbase/block-editor";
import { useStore } from "@/stores/root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import { SpaceMutations } from "@/mutations/space.mutations";

interface SpaceDocumentsPanelProps {
  spaceId: string;
}

export const SpaceDocumentsPanel = observer(function SpaceDocumentsPanel({ spaceId }: SpaceDocumentsPanelProps) {
  const rootStore = useStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  const space = rootStore.spaceStore.getById(spaceId);
  const { canCreateContent: canEdit, isAdmin: canManage } = useWorkspaceRole();

  if (!space) return null;

  const updateField = (patches: Parameters<SpaceMutations["updateLocal"]>[1]) => {
    spaceMutations.updateLocal(spaceId, patches).catch((err) => console.error("Failed to apply local space update", err));
    scheduleFlush();
  };

  return (
    <div className="flex flex-col h-full overflow-hidden">

      {/* Space hero header */}
      <div className="px-8 pt-8 pb-4 shrink-0">
        <div className="flex items-center gap-3">
          <UniversalPicker
            icon={space.icon || "LayoutGrid"}
            color={space.color || "#3b82f6"}
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

      {/* Divider */}
      <div className="mx-8 border-t border-border/15 shrink-0" />

      {/* Document — fills remaining space */}
      <div className="flex-1 overflow-y-auto px-8 py-4 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20 hover:[&::-webkit-scrollbar-thumb]:bg-muted-foreground/40 [&::-webkit-scrollbar-track]:bg-transparent">
        {space.defaultDocumentId ? (
          <BlockEditor key={space.defaultDocumentId} documentId={space.defaultDocumentId} editable={canEdit} />
        ) : (
          <p className="text-xs text-muted-foreground/40 px-1 mt-2">No document for this space yet.</p>
        )}
      </div>
    </div>
  );
});
