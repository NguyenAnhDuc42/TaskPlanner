import { useReducer } from "react";
import { useCreateSpaceMutation } from "../../contents/hierarchy/hierarchy-api";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "../../context/workspace-provider";
import { toast } from "sonner";
import { PrivacyToggle, IconColorPicker, AttributeButton } from "./form-elements";
import { User } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useUser } from "@/features/auth/api";
import { useSelector } from "react-redux";
import { memberSelectors } from "@/store/entityStore";
import type { MemberRecord } from "@/types/workspace";

interface CreateSpaceFormProps {
  readonly onSuccess?: (id: string) => void;
  readonly onCancel?: () => void;
}

interface SpaceFormState {
  readonly name: string;
  readonly isPrivate: boolean;
  readonly icon: string;
  readonly color: string;
  readonly selectedMemberIds: readonly string[];
}

type SpaceFormAction =
  | { readonly type: "SET_NAME"; readonly payload: string }
  | { readonly type: "SET_PRIVATE"; readonly payload: boolean }
  | { readonly type: "SET_ICON"; readonly payload: string }
  | { readonly type: "SET_COLOR"; readonly payload: string }
  | { readonly type: "TOGGLE_MEMBER"; readonly payload: string }
  | { readonly type: "RESET" };

const initialSpaceState: SpaceFormState = {
  name: "",
  isPrivate: false,
  icon: "LayoutGrid",
  color: "#6366f1",
  selectedMemberIds: [],
};

function spaceFormReducer(state: SpaceFormState, action: SpaceFormAction): SpaceFormState {
  switch (action.type) {
    case "SET_NAME":
      return { ...state, name: action.payload };
    case "SET_PRIVATE":
      return { ...state, isPrivate: action.payload };
    case "SET_ICON":
      return { ...state, icon: action.payload };
    case "SET_COLOR":
      return { ...state, color: action.payload };
    case "TOGGLE_MEMBER":
      return {
        ...state,
        selectedMemberIds: state.selectedMemberIds.includes(action.payload)
          ? state.selectedMemberIds.filter((id) => id !== action.payload)
          : [...state.selectedMemberIds, action.payload],
      };
    case "RESET":
      return initialSpaceState;
    default:
      return state;
  }
}

export function CreateSpaceForm({ onSuccess, onCancel }: Readonly<CreateSpaceFormProps>) {
  const { workspaceId } = useWorkspace();
  const allMembers = useSelector(memberSelectors.selectAll);
  const { data: currentUser } = useUser();
  const [createSpaceMutation, { isLoading: isCreating }] = useCreateSpaceMutation();
  const [state, dispatch] = useReducer(spaceFormReducer, initialSpaceState);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!state.name.trim()) return;

    try {
      const { id: newId } = await createSpaceMutation({ 
        workspaceId: workspaceId || "", 
        body: { 
          name: state.name, 
          isPrivate: state.isPrivate, 
          color: state.color, 
          icon: state.icon, 
          memberIdsToInvite: [...state.selectedMemberIds] 
        },
      }).unwrap();
      toast.success("Space created");
      
      dispatch({ type: "RESET" });

      onSuccess?.(newId);
    } catch (error) {
      console.error(error);
      toast.error("Failed to create space");
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
            placeholder="Space name"
            aria-label="Space name"
            onClick={e => e.stopPropagation()}
            value={state.name}
            onChange={(e) => dispatch({ type: "SET_NAME", payload: e.target.value })}
            className="flex-1 bg-transparent border-none focus:ring-0 text-[13px] font-semibold placeholder:text-muted-foreground/30 py-0 outline-none tracking-tight"
          />
        </div>
      </div>

      {/* Attribute Strip */}
      <div className="px-3 py-1.5 flex flex-nowrap items-center gap-1.5 border-t border-border/5 overflow-x-auto [&::-webkit-scrollbar]:hidden">
        <PrivacyToggle 
          isPrivate={state.isPrivate} 
          onChange={(val) => dispatch({ type: "SET_PRIVATE", payload: val })} 
        />
        
        <Popover>
          <PopoverTrigger asChild>
            <AttributeButton icon={User} className="ml-auto" active={state.selectedMemberIds.length > 0}>
              {state.selectedMemberIds.length > 0 ? `Members (${state.selectedMemberIds.length})` : "Members"}
            </AttributeButton>
          </PopoverTrigger>
          <PopoverContent className="w-52 p-1 bg-popover border border-border/40 shadow-lg rounded-md text-foreground" align="end">
            <div className="text-[9px] font-bold text-muted-foreground/60 p-1 border-b border-border/10 uppercase tracking-wider font-mono">
              Invite Workspace Members
            </div>
            <div className="max-h-[180px] overflow-y-auto mt-1 flex flex-col gap-0.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-white/[0.05]">
              {allMembers.reduce((acc: React.ReactNode[], member: MemberRecord) => {
                const isCurrentUser = member.email && currentUser?.email && member.email.toLowerCase() === currentUser.email.toLowerCase();
                if (!isCurrentUser) {
                  const targetId = member.id;
                  const isSelected = state.selectedMemberIds.includes(targetId);
                  const initials = (member.name || "U").split(" ").map((w: string) => w[0]).slice(0, 2).join("").toUpperCase();
                  acc.push(
                    <button
                      key={targetId}
                      type="button"
                      onClick={() => dispatch({ type: "TOGGLE_MEMBER", payload: targetId })}
                      className="flex items-center gap-1.5 p-1 rounded hover:bg-white/[0.03] transition-colors w-full text-left cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        readOnly
                        aria-label={`Invite ${member.name}`}
                        className="h-3 w-3 rounded border-border/40 accent-primary cursor-pointer"
                      />
                      <div className="h-4.5 w-4.5 rounded-full bg-primary/20 border border-border/20 flex items-center justify-center text-[8px] font-black text-white shrink-0">
                        {initials}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="font-bold text-[9px] text-foreground/90 truncate leading-none">{member.name}</div>
                        <div className="text-[7.5px] text-muted-foreground/45 truncate mt-0.5">{member.email}</div>
                      </div>
                    </button>
                  );
                }
                return acc;
              }, [])}
            </div>
          </PopoverContent>
        </Popover>
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
          Cancel
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
