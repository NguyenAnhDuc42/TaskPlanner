import { useState } from "react";
import { createPortal } from "react-dom";
import { GripVertical, Workflow, Trash2 } from "lucide-react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { pointerAwareCollisionDetection } from "@/lib/dnd-collision";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { DebouncedInput } from "@/components/debounced-input";
import { IconColorPicker } from "@/features/workspace/components/forms/form-elements";
import { cn } from "@/lib/utils";
import { SPACE_RAIL_TABS, type SpaceRailTabKey } from "./space-rail-tabs";
import { useLocalStorage } from "@/hooks/use-local-storage";

export const HIDE_EMPTY_DEFAULT_KEY = "space-board-hide-empty-default";

interface SpaceSettingsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  tabOrder: SpaceRailTabKey[];
  onTabOrderChange: (order: SpaceRailTabKey[]) => void;
  spaceName: string;
  onSpaceNameChange: (name: string) => void;
  spaceIcon: string;
  spaceColor: string;
  onSpaceIconChange: (icon: string, color: string) => void;
  pinnedTab: SpaceRailTabKey | null;
  onPinTabChange: (tab: SpaceRailTabKey | null) => void;
  onOpenWorkflow?: () => void;
  onDeleteSpace?: () => void;
}

function SettingsSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-[10px] font-semibold text-muted-foreground/60 uppercase tracking-wide">{title}</span>
      {children}
    </div>
  );
}

function TabRow({ tabKey, overlay = false }: { tabKey: SpaceRailTabKey; overlay?: boolean }) {
  const tab = SPACE_RAIL_TABS[tabKey];
  const Icon = tab.icon;
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: tabKey });

  const style = overlay ? {} : { transform: CSS.Transform.toString(transform), transition };

  return (
    <div
      ref={overlay ? undefined : setNodeRef}
      style={style}
      className={cn(
        "group relative flex items-center gap-2 h-8 px-2 rounded-md border bg-card/60 hover:bg-card/90 border-border/30 hover:border-border/60 transition-colors duration-150 select-none",
        isDragging && !overlay && "opacity-0",
        overlay && "shadow-2xl rotate-1 scale-105 opacity-90 bg-card border-border/60"
      )}
    >
      <button
        type="button"
        className="text-muted-foreground/20 hover:text-muted-foreground/60 cursor-grab active:cursor-grabbing shrink-0 touch-none transition-colors"
        {...(overlay ? {} : { ...attributes, ...listeners })}
      >
        <GripVertical className="h-3 w-3" />
      </button>
      <Icon className="h-3.5 w-3.5 text-muted-foreground/70 shrink-0" />
      <span className="text-[11px] font-semibold text-foreground/90">{tab.label}</span>
    </div>
  );
}

// Purely visual — no onClick of its own. The one place this is used already wraps it in a
// button that toggles on click; giving this its own onClick too double-fires on every click
// (bubbles up to the parent button as well), which visually looked like the switch didn't
// respond, since the two toggles cancelled each other out.
function Toggle({ checked }: { checked: boolean }) {
  return (
    <span
      className={cn(
        "relative inline-block h-4.5 w-8 rounded-full transition-colors shrink-0",
        checked ? "bg-primary" : "bg-muted-foreground/25"
      )}
    >
      <span
        className={cn(
          "absolute top-0.5 left-0.5 h-3.5 w-3.5 rounded-full bg-background transition-transform",
          checked ? "translate-x-3.5" : "translate-x-0"
        )}
      />
    </span>
  );
}

export function SpaceSettingsDialog({
  isOpen,
  onClose,
  tabOrder,
  onTabOrderChange,
  spaceName,
  onSpaceNameChange,
  spaceIcon,
  spaceColor,
  onSpaceIconChange,
  pinnedTab,
  onPinTabChange,
  onOpenWorkflow,
  onDeleteSpace,
}: Readonly<SpaceSettingsDialogProps>) {
  const [draggingKey, setDraggingKey] = useState<SpaceRailTabKey | null>(null);
  const [hideEmptyDefault, setHideEmptyDefault] = useLocalStorage(HIDE_EMPTY_DEFAULT_KEY, true);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 4 } }));

  const handleDragEnd = (event: DragEndEvent) => {
    setDraggingKey(null);
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIdx = tabOrder.indexOf(active.id as SpaceRailTabKey);
    const newIdx = tabOrder.indexOf(over.id as SpaceRailTabKey);
    if (oldIdx === -1 || newIdx === -1) return;
    onTabOrderChange(arrayMove(tabOrder, oldIdx, newIdx));
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) onClose(); }}>
      <DialogContent
        className="max-w-100 w-full bg-background border border-border/30 text-foreground p-0 rounded-xl overflow-hidden shadow-2xl"
        showCloseButton={false}
      >
        <DialogHeader className="px-5 py-3 border-b border-border/20 bg-card/30 backdrop-blur-sm shrink-0">
          <DialogTitle className="text-sm font-black text-foreground/90 tracking-tight">
            Space Settings
          </DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4 p-4 max-h-[70vh] overflow-y-auto [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
          {/* Name + Icon */}
          <SettingsSection title="Name">
            <div className="flex items-center gap-2">
              <IconColorPicker icon={spaceIcon} color={spaceColor} onChange={onSpaceIconChange} />
              <DebouncedInput
                value={spaceName}
                onChange={(val) => { if (val.trim()) onSpaceNameChange(val.trim()); }}
                debounceMs={800}
                placeholder="Space name"
                className="flex-1 h-8 px-2.5 text-[12px] font-semibold rounded-md border border-border/30 bg-muted/20 outline-none focus:border-primary/50 text-foreground"
              />
            </div>
          </SettingsSection>

          {/* Default tab */}
          <SettingsSection title="Default Tab">
            <div className="flex items-center gap-1 rounded-md border border-border/30 bg-card/60 p-1">
              {tabOrder.map((key) => {
                const tab = SPACE_RAIL_TABS[key];
                const Icon = tab.icon;
                const isPinned = pinnedTab === key;
                return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => onPinTabChange(isPinned ? null : key)}
                    title={isPinned ? `Unpin ${tab.label}` : `Always open on ${tab.label}`}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-1 h-6 rounded transition-colors cursor-pointer",
                      isPinned ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                    )}
                  >
                    <Icon className="h-3 w-3" />
                    <span className="text-[9px] font-semibold">{tab.label}</span>
                  </button>
                );
              })}
            </div>
            <p className="text-[9px] text-muted-foreground/40">
              {pinnedTab ? `Always opens on ${SPACE_RAIL_TABS[pinnedTab].label}.` : "Opens on the last tab used, workspace-wide."}
            </p>
          </SettingsSection>

          {/* Tab order */}
          <SettingsSection title="Tab Order">
            <DndContext
              sensors={sensors}
              collisionDetection={pointerAwareCollisionDetection}
              modifiers={[restrictToVerticalAxis]}
              onDragStart={(e: DragStartEvent) => setDraggingKey(e.active.id as SpaceRailTabKey)}
              onDragEnd={handleDragEnd}
            >
              <SortableContext items={tabOrder} strategy={verticalListSortingStrategy}>
                <div className="flex flex-col gap-1.5">
                  {tabOrder.map((key) => (
                    <TabRow key={key} tabKey={key} />
                  ))}
                </div>
              </SortableContext>

              {createPortal(
                <DragOverlay dropAnimation={null}>
                  {draggingKey ? <TabRow tabKey={draggingKey} overlay /> : null}
                </DragOverlay>,
                document.body
              )}
            </DndContext>
            {draggingKey && (
              <p className="text-[9px] text-muted-foreground/40">Drag to reorder</p>
            )}
          </SettingsSection>

          {/* Board defaults */}
          <SettingsSection title="Board">
            <button
              type="button"
              onClick={() => setHideEmptyDefault(!hideEmptyDefault)}
              className="flex items-center justify-between h-8 px-2.5 rounded-md border border-border/30 bg-card/60 hover:bg-card/90 transition-colors"
            >
              <span className="text-[11px] font-semibold text-foreground/90">Hide empty columns by default</span>
              <Toggle checked={hideEmptyDefault} />
            </button>
          </SettingsSection>

          {/* Statuses */}
          {onOpenWorkflow && (
            <SettingsSection title="Statuses">
              <button
                type="button"
                onClick={onOpenWorkflow}
                className="flex items-center justify-between h-8 px-2.5 rounded-md border border-border/30 bg-card/60 hover:bg-card/90 transition-colors cursor-pointer"
              >
                <span className="flex items-center gap-1.5 text-[11px] font-semibold text-foreground/90">
                  <Workflow className="h-3.5 w-3.5 text-muted-foreground/70" />
                  Manage Statuses
                </span>
              </button>
            </SettingsSection>
          )}

          {/* Danger zone */}
          {onDeleteSpace && (
            <SettingsSection title="Danger Zone">
              <button
                type="button"
                onClick={onDeleteSpace}
                className="flex items-center justify-between h-8 px-2.5 rounded-md border border-destructive/20 bg-destructive/5 hover:bg-destructive/10 transition-colors cursor-pointer"
              >
                <span className="flex items-center gap-1.5 text-[11px] font-semibold text-destructive">
                  <Trash2 className="h-3.5 w-3.5" />
                  Delete Space
                </span>
              </button>
            </SettingsSection>
          )}
        </div>

        <div className="flex items-center justify-end px-5 py-2.5 border-t border-border/15 bg-card/20 backdrop-blur-sm shrink-0">
          <button
            type="button"
            onClick={onClose}
            className="h-7 px-3 rounded-md bg-primary/90 hover:bg-primary text-primary-foreground text-[11px] font-bold transition-all active:scale-95 shadow-sm"
          >
            Done
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
