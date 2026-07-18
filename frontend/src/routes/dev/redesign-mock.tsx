import { useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import {
  Orbit,
  Search,
  Inbox,
  ListTodo,
  Star,
  Plus,
  ChevronRight,
  ChevronsUpDown,
  FileText,
  History,
  MoreVertical,
  Circle,
  Flag,
  MessageSquare,
} from "lucide-react";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/dev/redesign-mock")({
  component: RedesignMock,
});

type TabKey = "docs" | "tasks" | "activity";

interface DocNode {
  id: string;
  name: string;
  children?: DocNode[];
}

const PROJECTS = [
  { id: "p1", name: "Orpus App", color: "#ffffff" },
  { id: "p2", name: "Marketing Site", color: "#f59e0b" },
  { id: "p3", name: "Learning C#", color: "#10b981" },
];

const FAVORITES = [
  { id: "f1", name: "Sync Engine Notes", icon: FileText },
  { id: "f2", name: "Fix auth refresh", icon: Circle },
];

const DOC_TREE: DocNode[] = [
  { id: "d1", name: "Overview" },
  {
    id: "d2",
    name: "Architecture",
    children: [
      { id: "d21", name: "Sync Engine" },
      { id: "d22", name: "Data Model" },
      { id: "d23", name: "Document Blocks" },
    ],
  },
  {
    id: "d3",
    name: "Guides",
    children: [{ id: "d31", name: "Deployment" }],
  },
  { id: "d4", name: "Meeting Notes" },
];

const BOARD = [
  {
    label: "Todo",
    color: "#6b7280",
    cards: [
      { name: "Command palette (⌘K)", priority: "#f59e0b", date: "Jul 22" },
      { name: "Doc tree drag & drop", priority: "#ef4444", date: "Jul 25" },
    ],
  },
  {
    label: "In Progress",
    color: "#3b82f6",
    cards: [
      { name: "Sidebar restructure", priority: "#ef4444", date: "Jul 18" },
      { name: "Milestone entity", priority: "#6b7280", date: "Jul 20" },
    ],
  },
  {
    label: "Done",
    color: "#10b981",
    cards: [
      { name: "Savepoint batch flush", priority: "#6b7280", date: "Jul 16" },
      { name: "Fractional order keys", priority: "#f59e0b", date: "Jul 16" },
      { name: "LRU doc cache", priority: "#6b7280", date: "Jul 17" },
    ],
  },
];

const MILESTONES = [
  { name: "v0.5 — Sync Hardening", due: "Jul 16", done: 9, total: 10 },
  { name: "v0.6 — Project-centric UI", due: "Aug 10", done: 2, total: 8 },
];

const UPDATES = [
  { author: "Duc", time: "2h ago", text: "Shipped the savepoint batch flush — trace went from 160 spans to ~50 and the poison queue self-cleans now." },
  { author: "Duc", time: "yesterday", text: "Decided: Space → Project rename, doc tree per project, no separate Note entity. Docs are for knowing, tasks are for doing." },
  { author: "NA", time: "2d ago", text: "Board feels way faster after the memory pass 👍" },
];

function DocTreeItem({ node, depth, activeId, onSelect }: Readonly<{ node: DocNode; depth: number; activeId: string; onSelect: (id: string) => void }>) {
  const [open, setOpen] = useState(true);
  const hasChildren = !!node.children?.length;

  return (
    <>
      <button
        type="button"
        onClick={() => onSelect(node.id)}
        className={cn(
          "w-full flex items-center gap-1 py-1 pr-2 rounded-md text-left transition-colors cursor-pointer",
          activeId === node.id ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
        )}
        style={{ paddingLeft: 8 + depth * 14 }}
      >
        {hasChildren ? (
          <ChevronRight
            className={cn("h-3 w-3 shrink-0 transition-transform", open && "rotate-90")}
            onClick={(e) => {
              e.stopPropagation();
              setOpen((o) => !o);
            }}
          />
        ) : (
          <FileText className="h-3 w-3 shrink-0 opacity-50" />
        )}
        <span className="text-[11px] font-medium truncate">{node.name}</span>
      </button>
      {hasChildren && open &&
        node.children!.map((child) => (
          <DocTreeItem key={child.id} node={child} depth={depth + 1} activeId={activeId} onSelect={onSelect} />
        ))}
    </>
  );
}

function RedesignMock() {
  const [activeProject, setActiveProject] = useState("p1");
  const [activeTab, setActiveTab] = useState<TabKey>("docs");
  const [activeDoc, setActiveDoc] = useState("d21");

  const project = PROJECTS.find((p) => p.id === activeProject)!;

  return (
    <div className="flex h-screen w-full bg-background font-sans overflow-hidden text-foreground p-1 gap-1">
      <aside className="w-60 shrink-0 flex flex-col bg-card border border-border rounded-md shadow-sm overflow-hidden">
        <div className="flex items-center justify-between px-2 h-10 shrink-0">
          <button type="button" className="flex items-center gap-1.5 px-1.5 py-1 rounded-md hover:bg-muted/50 transition-colors cursor-pointer min-w-0">
            <div className="h-4.5 w-4.5 rounded flex items-center justify-center bg-primary/20 shrink-0">
              <Orbit className="h-3 w-3 text-primary" />
            </div>
            <span className="text-[11px] font-bold truncate">Orpus</span>
            <ChevronsUpDown className="h-3 w-3 text-muted-foreground/60 shrink-0" />
          </button>
          <button type="button" className="h-6 w-6 rounded-full bg-primary/80 text-[9px] font-bold text-primary-foreground flex items-center justify-center shrink-0 cursor-pointer">
            NA
          </button>
        </div>

        <div className="px-2 pb-2 shrink-0">
          <button type="button" className="w-full flex items-center gap-2 h-7 px-2 rounded-md bg-secondary/60 hover:bg-secondary/80 transition-colors cursor-pointer">
            <Search className="h-3 w-3 text-muted-foreground/60" />
            <span className="text-[11px] text-muted-foreground/50 font-medium">Search…</span>
            <kbd className="ml-auto text-[9px] text-muted-foreground/40 font-semibold">⌘K</kbd>
          </button>
        </div>

        <nav className="px-2 flex flex-col gap-0.5 shrink-0">
          <button type="button" className="flex items-center gap-2 h-7 px-2 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer">
            <Inbox className="h-3.5 w-3.5" />
            <span className="text-[11px] font-semibold">Inbox</span>
            <span className="ml-auto h-4 min-w-4 px-1 rounded-full bg-primary/15 text-primary text-[9px] font-bold flex items-center justify-center">3</span>
          </button>
          <button type="button" className="flex items-center gap-2 h-7 px-2 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer">
            <ListTodo className="h-3.5 w-3.5" />
            <span className="text-[11px] font-semibold">My Tasks</span>
          </button>
        </nav>

        <div className="px-2 mt-3 shrink-0">
          <p className="px-2 pb-1 text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Favorites</p>
          <div className="flex flex-col gap-0.5">
            {FAVORITES.map((fav) => (
              <button key={fav.id} type="button" className="flex items-center gap-2 h-6.5 px-2 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer">
                <fav.icon className="h-3 w-3 text-white" />
                <span className="text-[11px] font-medium truncate">{fav.name}</span>
                <Star className="ml-auto h-2.5 w-2.5 fill-amber-400/80 text-amber-400/80 shrink-0" />
              </button>
            ))}
          </div>
        </div>

        <div className="px-2 mt-3 flex-1 min-h-0 overflow-y-auto [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
          <div className="flex items-center justify-between px-2 pb-1">
            <p className="text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Projects</p>
            <Plus className="h-3 w-3 text-muted-foreground/40 hover:text-primary cursor-pointer" />
          </div>
          <div className="flex flex-col gap-0.5">
            {PROJECTS.map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => setActiveProject(p.id)}
                className={cn(
                  "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer",
                  activeProject === p.id ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
                )}
              >
                <Orbit className="h-3.5 w-3.5" style={{ color: p.color }} />
                <span className="text-[11px] font-semibold truncate">{p.name}</span>
              </button>
            ))}
          </div>
        </div>
      </aside>

      <main className="flex-1 min-w-0 flex flex-col bg-card border border-border rounded-md shadow-sm overflow-hidden">
        <div className="flex items-center gap-1 h-9 px-2 border-b border-border shrink-0">
          <div className="flex items-center gap-1.5 text-xs min-w-0">
            <Orbit className="h-3.5 w-3.5 shrink-0" style={{ color: project.color }} />
            <span className="font-semibold text-foreground/80 truncate max-w-48">{project.name}</span>
            <Star className="h-3 w-3 text-muted-foreground/40 hover:text-amber-400 cursor-pointer shrink-0" />
          </div>
          <div className="h-4 w-px bg-border/60 mx-1.5 shrink-0" />
          {(
            [
              { key: "docs", label: "Docs", icon: FileText },
              { key: "tasks", label: "Tasks", icon: ListTodo },
              { key: "activity", label: "Activity", icon: History },
            ] as const
          ).map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key)}
              className={cn(
                "flex items-center gap-1.5 h-7 px-2 rounded-md transition-colors cursor-pointer",
                activeTab === tab.key ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
              )}
            >
              <tab.icon className="h-3.5 w-3.5 shrink-0" />
              <span className="text-[10px] font-semibold">{tab.label}</span>
            </button>
          ))}
          <button type="button" className="ml-auto flex items-center h-7 px-1.5 rounded-md text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors cursor-pointer">
            <MoreVertical className="h-3.5 w-3.5" />
          </button>
        </div>

        {activeTab === "docs" && (
          <div className="flex-1 flex overflow-hidden">
            <div className="w-52 shrink-0 border-r border-border/40 flex flex-col overflow-hidden">
              <div className="flex items-center justify-between px-3 h-8 shrink-0">
                <p className="text-[9px] font-bold uppercase tracking-wider text-muted-foreground/40">Documents</p>
                <Plus className="h-3 w-3 text-muted-foreground/40 hover:text-primary cursor-pointer" />
              </div>
              <div className="flex-1 overflow-y-auto px-1.5 pb-2 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
                {DOC_TREE.map((node) => (
                  <DocTreeItem key={node.id} node={node} depth={0} activeId={activeDoc} onSelect={setActiveDoc} />
                ))}
              </div>
            </div>
            <div className="flex-1 overflow-y-auto px-10 py-8 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
              <h1 className="text-2xl font-black mb-1">Sync Engine</h1>
              <p className="text-[10px] text-muted-foreground/50 mb-6">Last edited 2h ago · 14 blocks</p>
              <div className="space-y-3 max-w-2xl text-[13px] leading-relaxed text-foreground/85">
                <p>The sync engine keeps the local IndexedDB copy and the server reconciled through an append-only event log. Every edit lands locally first, then flushes in batches.</p>
                <h2 className="text-base font-bold pt-2">Write path</h2>
                <p>Mutations write the MobX store and IndexedDB synchronously, enqueue a transaction, and a debounced flush posts the squashed queue to <span className="px-1 py-0.5 rounded bg-muted/60 text-[12px] font-mono">/sync/batch</span>.</p>
                <h2 className="text-base font-bold pt-2">Read path</h2>
                <p>SignalR pushes Delta and DeltaBatch messages; the delta handler applies them to the store and IndexedDB. Own echoes are skipped by content digest.</p>
                <div className="border border-border/40 rounded-md p-3 text-[12px] text-muted-foreground bg-muted/20">
                  Referenced by: <span className="text-primary">Fix auth refresh</span> · <span className="text-primary">Deployment</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {activeTab === "tasks" && (
          <div className="flex-1 flex gap-2 px-2 py-2 overflow-x-auto overflow-y-hidden [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            {BOARD.map((col) => (
              <div key={col.label} className="w-[260px] shrink-0 flex flex-col rounded-md bg-muted/20 border border-border/30 overflow-hidden">
                <div className="flex items-center gap-2 px-3 h-8 shrink-0">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: col.color }} />
                  <span className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">{col.label}</span>
                  <span className="text-[10px] text-muted-foreground/40 font-semibold">{col.cards.length}</span>
                </div>
                <div className="flex-1 overflow-y-auto px-2 pb-2 flex flex-col gap-1.5 [&::-webkit-scrollbar]:w-1 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
                  {col.cards.map((card) => (
                    <div key={card.name} className="rounded-lg border border-border/60 bg-card p-2 shadow-sm hover:border-border cursor-pointer">
                      <div className="flex items-center gap-1.5">
                        <Circle className="h-3 w-3 text-white shrink-0" />
                        <span className="text-[12px] font-medium truncate">{card.name}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-2">
                        <Flag className="h-2.5 w-2.5" style={{ color: card.priority }} />
                        <span className="text-[9px] text-muted-foreground/50 font-semibold">{card.date}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}

        {activeTab === "activity" && (
          <div className="flex-1 overflow-y-auto px-10 py-6 [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-muted-foreground/20">
            <div className="max-w-2xl space-y-8">
              <section>
                <h2 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground/50 mb-3">Milestones</h2>
                <div className="space-y-2">
                  {MILESTONES.map((m) => (
                    <div key={m.name} className="rounded-lg border border-border/40 bg-muted/20 p-3">
                      <div className="flex items-center justify-between">
                        <span className="text-[12px] font-semibold">{m.name}</span>
                        <span className="text-[10px] text-muted-foreground/50 font-semibold">{m.due}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-2">
                        <div className="flex-1 h-1 rounded-full bg-muted/60 overflow-hidden">
                          <div className="h-full bg-primary/70 rounded-full" style={{ width: `${(m.done / m.total) * 100}%` }} />
                        </div>
                        <span className="text-[10px] text-muted-foreground/60 font-semibold shrink-0">{m.done}/{m.total}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </section>

              <section>
                <h2 className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground/50 mb-3">Updates</h2>
                <div className="space-y-3">
                  {UPDATES.map((u, i) => (
                    <div key={i} className="flex gap-2.5">
                      <div className="h-6 w-6 rounded-full bg-primary/70 text-[9px] font-bold text-primary-foreground flex items-center justify-center shrink-0 mt-0.5">
                        {u.author.slice(0, 2).toUpperCase()}
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-baseline gap-2">
                          <span className="text-[11px] font-bold">{u.author}</span>
                          <span className="text-[9px] text-muted-foreground/40 font-semibold">{u.time}</span>
                        </div>
                        <p className="text-[12px] text-foreground/85 leading-relaxed">{u.text}</p>
                      </div>
                    </div>
                  ))}
                </div>
                <button type="button" className="mt-4 w-full flex items-center gap-2 h-8 px-3 rounded-md border border-dashed border-border/50 text-muted-foreground/50 hover:text-foreground hover:border-border transition-colors cursor-pointer">
                  <MessageSquare className="h-3 w-3" />
                  <span className="text-[11px] font-medium">Post an update…</span>
                </button>
              </section>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
