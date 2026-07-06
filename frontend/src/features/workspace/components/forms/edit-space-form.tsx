import { useMemo, useReducer } from "react";
import { observer } from "mobx-react-lite";
import { extractErrorMessage } from "@/types/api-error";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { IconColorPicker } from "./form-elements";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";

interface EditSpaceFormProps {
  readonly spaceId: string;
  readonly onSuccess?: () => void;
  readonly onCancel?: () => void;
}

interface SpaceEditState {
  readonly name: string;
  readonly icon: string;
  readonly color: string;
}

type SpaceEditAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string };

function spaceEditReducer(state: SpaceEditState, action: SpaceEditAction): SpaceEditState {
  switch (action.type) {
    case "SET_NAME":  return { ...state, name: action.payload };
    case "SET_ICON":  return { ...state, icon: action.payload };
    case "SET_COLOR": return { ...state, color: action.payload };
    default:          return state;
  }
}

// Edits the space itself (name/icon/color) — distinct from CreateSpaceForm, which only ever
// creates a new space. The "Space Settings" menu item used to open CreateSpaceForm by mistake,
// which meant "editing" a space actually created an unrelated new one.
export const EditSpaceForm = observer(function EditSpaceForm({ spaceId, onSuccess, onCancel }: Readonly<EditSpaceFormProps>) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const space = rootStore.spaceStore.getById(spaceId);
  const [isSaving, setIsSaving] = useReducer((_: boolean, v: boolean) => v, false);
  const [state, dispatch] = useReducer(spaceEditReducer, {
    name: space?.name ?? "",
    icon: space?.icon ?? "LayoutGrid",
    color: space?.color ?? "#6366f1",
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!state.name.trim() || !space) return;

    setIsSaving(true);
    try {
      await spaceMutations.update(spaceId, {
        name: state.name,
        icon: state.icon,
        color: state.color,
      });
      toast.success("Space updated");
      onSuccess?.();
    } catch (error) {
      toast.error(extractErrorMessage(error, "Failed to update space"));
    } finally {
      setIsSaving(false);
    }
  };

  if (!space) return null;

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full" onClick={(e) => e.stopPropagation()}>
      <div className="px-3 pt-2.5 pb-2">
        <div className="flex items-center gap-3">
          <IconColorPicker
            icon={state.icon}
            color={state.color}
            onChange={(i, c) => {
              dispatch({ type: "SET_ICON", payload: i });
              dispatch({ type: "SET_COLOR", payload: c });
            }}
          />
          <input
            placeholder="Space name"
            aria-label="Space name"
            onClick={e => e.stopPropagation()}
            value={state.name}
            onChange={(e) => dispatch({ type: "SET_NAME", payload: e.target.value })}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
          />
        </div>
      </div>

      <div className="px-3 py-1.5 bg-background flex items-center justify-end gap-2 border-t border-border/10">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onCancel}
          className="h-7 px-2.5 text-[10px] font-medium text-muted-foreground hover:text-foreground"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          size="sm"
          disabled={!state.name.trim() || isSaving}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {isSaving ? "Saving..." : "Save Changes"}
        </Button>
      </div>
    </form>
  );
});
