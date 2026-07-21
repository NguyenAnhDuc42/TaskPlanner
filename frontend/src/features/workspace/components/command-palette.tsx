import { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { Command } from "cmdk";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { DynamicIcon } from "@/components/dynamic-icon";
import { Inbox as InboxIcon, ListTodo, Users, Search } from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspace } from "../context/workspace-context";
import { EntityLayerType } from "@/types/entity-layer-type";
import { useWorkspaceSearch } from "./use-workspace-search";

const NAV_ACTIONS = [
  { id: "inbox", label: "Go to Inbox", icon: InboxIcon, to: "/workspaces/$workspaceId/inbox" },
  { id: "my-tasks", label: "Go to My Tasks", icon: ListTodo, to: "/workspaces/$workspaceId/my-tasks" },
  { id: "members", label: "Go to Members", icon: Users, to: "/workspaces/$workspaceId/members" },
] as const;

function defaultIcon(type: EntityLayerType) {
  if (type === EntityLayerType.ProjectTask) return "Circle";
  if (type === EntityLayerType.ProjectDocument) return "FileText";
  return "Orbit";
}

export const CommandPalette = observer(function CommandPalette() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const navigate = useNavigate();
  const { workspaceId } = useWorkspace();
  const { sections, navigateToResult } = useWorkspaceSearch(query);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key.toLowerCase() === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setOpen((prev) => !prev);
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, []);

  useEffect(() => {
    if (!open) setQuery("");
  }, [open]);

  const q = query.trim().toLowerCase();
  const visibleActions = NAV_ACTIONS.filter((a) => !q || a.label.toLowerCase().includes(q));

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent showCloseButton={false} className="p-0 overflow-hidden sm:max-w-lg top-[20%] translate-y-0">
        <DialogTitle className="sr-only">Command palette</DialogTitle>
        <Command shouldFilter={false} className="flex flex-col">
          <div className="flex items-center gap-2 px-3 border-b border-border/60">
            <Search className="h-3.5 w-3.5 text-muted-foreground/60 shrink-0" />
            <Command.Input
              autoFocus
              value={query}
              onValueChange={setQuery}
              placeholder="Search spaces, tasks, docs, or jump somewhere..."
              className="w-full h-10 bg-transparent text-[12px] font-medium placeholder:text-muted-foreground/50 outline-none"
            />
          </div>
          <Command.List className="max-h-80 overflow-y-auto p-1.5 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            <Command.Empty className="flex flex-col items-center justify-center text-center py-8 text-muted-foreground">
              <Search className="h-5 w-5 mb-1.5 opacity-20" />
              <p className="text-xs font-semibold text-foreground/70">No matches</p>
            </Command.Empty>

            {visibleActions.length > 0 && (
              <Command.Group
                heading="Go to"
                className="px-2 pb-1 [&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-bold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-muted-foreground/50 [&_[cmdk-group-heading]]:px-0 [&_[cmdk-group-heading]]:pb-1"
              >
                {visibleActions.map((action) => (
                  <Command.Item
                    key={action.id}
                    value={action.id}
                    onSelect={() => {
                      if (!workspaceId) return;
                      navigate({ to: action.to, params: { workspaceId } });
                      setOpen(false);
                    }}
                    className="flex items-center gap-2 px-2 py-1.5 rounded text-left cursor-pointer data-[selected=true]:bg-muted transition-colors"
                  >
                    <action.icon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                    <span className="text-[12px] font-medium">{action.label}</span>
                  </Command.Item>
                ))}
              </Command.Group>
            )}

            {sections.map((section) => (
              <Command.Group
                key={section.label}
                heading={section.label}
                className="px-2 pb-1 mt-1.5 pt-1.5 border-t border-border/40 [&_[cmdk-group-heading]]:text-[9px] [&_[cmdk-group-heading]]:font-bold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider [&_[cmdk-group-heading]]:text-muted-foreground/50 [&_[cmdk-group-heading]]:px-0 [&_[cmdk-group-heading]]:pb-1"
              >
                {section.results.map((result) => (
                  <Command.Item
                    key={`${result.type}-${result.id}`}
                    value={`${result.type}-${result.id}`}
                    onSelect={() => {
                      navigateToResult(result);
                      setOpen(false);
                    }}
                    className="flex items-center gap-2 px-2 py-1.5 rounded text-left cursor-pointer data-[selected=true]:bg-muted transition-colors"
                  >
                    <DynamicIcon
                      name={result.icon ?? defaultIcon(result.type)}
                      size={14}
                      color={result.color || "#ffffff"}
                      className="shrink-0"
                    />
                    <span className="text-[12px] font-medium truncate">{result.name}</span>
                  </Command.Item>
                ))}
              </Command.Group>
            ))}
          </Command.List>
        </Command>
      </DialogContent>
    </Dialog>
  );
});
