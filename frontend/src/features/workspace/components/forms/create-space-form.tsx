import { useMemo, useReducer, useRef } from "react";
import { extractErrorMessage } from "@/types/api-error";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { IconColorPicker } from "./form-elements";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { useSyncEngine } from "@/sync/sync-provider";
import { SpaceMutations } from "@/mutations/space.mutations";

interface CreateSpaceFormProps {
  readonly onSuccess?: (id: string) => void;
  readonly onCancel?: () => void;
}

interface SpaceFormState {
  readonly name: string;
  readonly icon: string;
  readonly color: string;
}

type SpaceFormAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string }
  | { readonly type: "RESET" };

const initialSpaceState: SpaceFormState = {
  name: "",
  icon: "LayoutGrid",
  color: "#6366f1",
};

function spaceFormReducer(state: SpaceFormState, action: SpaceFormAction): SpaceFormState {
  switch (action.type) {
    case "SET_NAME":
      return { ...state, name: action.payload };
    case "SET_ICON":
      return { ...state, icon: action.payload };
    case "SET_COLOR":
      return { ...state, color: action.payload };
    case "RESET":
      return initialSpaceState;
    default:
      return state;
  }
}

export function CreateSpaceForm({ onSuccess, onCancel }: Readonly<CreateSpaceFormProps>) {
  const rootStore = useWorkspaceRootStore();
  const syncEngine = useSyncEngine();
  const spaceMutations = useMemo(() => new SpaceMutations(rootStore, syncEngine), [rootStore, syncEngine]);
  const [isCreating, setIsCreating] = useReducer((_: boolean, v: boolean) => v, false);
  const [state, dispatch] = useReducer(spaceFormReducer, initialSpaceState);
  const nameInputRef = useRef<HTMLInputElement>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!state.name.trim()) return;

    setIsCreating(true);
    try {
      const record = await spaceMutations.create({
        name: state.name,
        isPrivate: false,
        color: state.color,
        icon: state.icon,
      });
      toast.success("Space created");

      // Stays open — reset for the next one instead of closing, so creating several spaces in a
      // row doesn't mean reopening the dialog each time.
      dispatch({ type: "RESET" });
      nameInputRef.current?.focus();

      onSuccess?.(record.id);
    } catch (error) {
      console.error(error);
      toast.error(extractErrorMessage(error, "Failed to create space"));
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col w-full" onClick={(e) => e.stopPropagation()}>
      {/* Main Header / Input Section */}
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
            ref={nameInputRef}
            placeholder="Space name"
            aria-label="Space name"
            onClick={e => e.stopPropagation()}
            value={state.name}
            onChange={(e) => dispatch({ type: "SET_NAME", payload: e.target.value })}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
          />
        </div>
      </div>

      {/* Footer */}
      <div className="px-3 py-1.5 bg-background flex items-center justify-end gap-2 border-t border-border/10">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onCancel}
          className="h-7 px-2.5 text-[10px] font-medium text-muted-foreground hover:text-foreground"
        >
          Done
        </Button>
        <Button
          type="submit"
          size="sm"
          disabled={!state.name.trim() || isCreating}
          className="h-7 px-4 text-[10px] font-semibold bg-primary hover:bg-primary/90 text-primary-foreground shadow-sm rounded-md transition-all active:scale-95"
        >
          {isCreating ? "Creating..." : "Create Space"}
        </Button>
      </div>
    </form>
  );
}
