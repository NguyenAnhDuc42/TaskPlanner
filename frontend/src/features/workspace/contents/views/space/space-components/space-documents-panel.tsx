import { Lock, Unlock } from "lucide-react";
import { useSpaceDetail, useUpdateSpaceFieldMutation } from "../space-api";
import { useSpaceAccess } from "@/features/workspace/context/use-space-access";
import { UniversalPicker } from "@/components/universal-picker";
import { BlockEditor } from "@/components/blockbase/block-editor";

interface SpaceDocumentsPanelProps {
  spaceId: string;
}

export function SpaceDocumentsPanel({ spaceId }: SpaceDocumentsPanelProps) {
  const space = useSpaceDetail(spaceId);
  const { canEdit, canManage } = useSpaceAccess(spaceId);
  const [updateSpaceField] = useUpdateSpaceFieldMutation();

  if (!space) return null;

  return (
    <div className="flex flex-col h-full overflow-hidden">

      {/* Space hero header */}
      <div className="px-8 pt-8 pb-4 shrink-0">
        <div className="flex items-center gap-3">
          <UniversalPicker
            icon={space.icon || "LayoutGrid"}
            color={space.color || "#3b82f6"}
            onSelect={(icon, color) => updateSpaceField({ spaceId, patches: { icon, color } })}
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
                  updateSpaceField({ spaceId, patches: { name: e.target.value.trim() } });
              }}
              onKeyDown={e => { if (e.key === "Enter") e.currentTarget.blur(); }}
            />
            <div className="px-1 mt-0.5">
              {space.isPrivate ? (
                <span className="flex items-center gap-0.5 text-[9px] font-bold text-rose-400/70 uppercase tracking-wider">
                  <Lock className="h-2.5 w-2.5" /> Private
                </span>
              ) : (
                <span className="flex items-center gap-0.5 text-[9px] font-bold text-emerald-400/70 uppercase tracking-wider">
                  <Unlock className="h-2.5 w-2.5" /> Public
                </span>
              )}
            </div>
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
}
