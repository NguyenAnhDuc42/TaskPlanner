import { useSpaceDetail } from "../space-api";
import {
  Users,
  Folder,
  Plus,
  PenBox,
  FileText,
  FileCode,
  LayoutTemplate,
  Notebook
} from "lucide-react";
import { format } from "date-fns";
import React, { useState } from "react";
import { StatusBadge } from "@/components/status-badge";

interface SpaceDetailProps {
  spaceId: string;
}

export function SpaceDetail({ spaceId }: SpaceDetailProps) {
  const space = useSpaceDetail(spaceId);
  const [activeTab, setActiveTab] = useState("overview");

  if (!space) return null;

  const tabContents: Record<string, React.ReactNode> = {
    overview: (
      <div className="prose prose-sm dark:prose-invert max-w-none text-muted-foreground/80 space-y-3">
        <p>Welcome to the project overview canvas. This section centralizes high-level specifications and references.</p>
        <ul className="list-disc pl-5 space-y-1 text-xs">
          <li>Figma link configured in assets tab</li>
          <li>Initial sprint target dates synced to properties</li>
          <li>Workflow pipeline mapped directly to space detail</li>
        </ul>
      </div>
    ),
    specs: (
      <div className="space-y-4">
        <h4 className="text-xs font-bold text-foreground/95">Project Scope Specifications</h4>
        <div className="border border-border/30 rounded-lg overflow-hidden bg-card/10">
          <table className="w-full text-xs text-left">
            <thead className="bg-white/[0.02] border-b border-border/20 text-muted-foreground/70 font-mono text-[9px] uppercase tracking-wider">
              <tr>
                <th className="p-2.5 font-semibold">Milestone Target</th>
                <th className="p-2.5 font-semibold">Owner</th>
                <th className="p-2.5 font-semibold">Delivery Date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/10 text-muted-foreground/90 font-medium">
              <tr>
                <td className="p-2.5 text-foreground/80">Redesign wireframes</td>
                <td className="p-2.5">Anh Đức Nguyễn</td>
                <td className="p-2.5 text-sky-400">June 14th</td>
              </tr>
              <tr>
                <td className="p-2.5 text-foreground/80">Setup API pipelines</td>
                <td className="p-2.5">System Engineer</td>
                <td className="p-2.5 text-amber-500">June 20th</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    ),
    figma: (
      <div className="p-6 border border-dashed border-border/40 rounded-xl bg-card/15 flex flex-col items-center justify-center text-center gap-3">
        <LayoutTemplate className="h-6 w-6 text-muted-foreground/40 animate-pulse" />
        <div>
          <h4 className="text-xs font-bold text-foreground">Figma Embedding Available</h4>
          <p className="text-[11px] text-muted-foreground/60 mt-1">Connect your workspace Figma account to live-embed layout mockups here.</p>
        </div>
        <button className="h-6 px-3 rounded-md bg-primary text-primary-foreground text-[10px] font-bold hover:bg-primary/90 transition-all cursor-pointer">
          Integrate Figma
        </button>
      </div>
    ),
    notes: (
      <div className="space-y-3.5 text-xs text-muted-foreground/80">
        <div className="flex items-start gap-2.5 p-2 rounded-lg bg-white/[0.01] border border-white/[0.03]">
          <span className="font-black text-amber-500 shrink-0">·</span>
          <span><strong className="text-foreground/95">Kickoff Meeting (May 29)</strong>: Set target goals, mapped column properties to status badge groups, and established custom transitions.</span>
        </div>
      </div>
    ),
  };

  return (
    <div className="flex h-full w-full bg-transparent overflow-hidden text-foreground">
      
      {/* 1. Left Column: Main Details Canvas */}
      <div className="flex-1 overflow-y-auto p-2 space-y-2 relative [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-white/[0.15]">
        
        {/* Tabbed Documents Box (Styled cleanly inside a border box) */}
        <div className="rounded-xl border border-border/30 overflow-hidden bg-card/15 shadow-sm">
          {/* Document Tabs selector header */}
          <div className="flex items-center gap-1 bg-white/[0.02] border-b border-border/20 px-2.5 pt-1.5 select-none">
            <button
              onClick={() => setActiveTab("overview")}
              className={`flex items-center gap-1 pb-1.5 text-[10px] font-bold border-b-2 px-1 transition-all cursor-pointer ${
                activeTab === "overview"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground/45 hover:text-foreground"
              }`}
            >
              <FileText className="h-3 w-3" />
              <span>Overview</span>
            </button>

            <button
              onClick={() => setActiveTab("specs")}
              className={`flex items-center gap-1 pb-1.5 text-[10px] font-bold border-b-2 px-1 transition-all cursor-pointer ${
                activeTab === "specs"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground/50 hover:text-foreground"
              }`}
            >
              <FileCode className="h-3 w-3" />
              <span>Specifications</span>
            </button>

            <button
              onClick={() => setActiveTab("figma")}
              className={`flex items-center gap-1 pb-1.5 text-[10px] font-bold border-b-2 px-1 transition-all cursor-pointer ${
                activeTab === "figma"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground/50 hover:text-foreground"
              }`}
            >
              <LayoutTemplate className="h-3 w-3" />
              <span>Figma Mockups</span>
            </button>

            <button
              onClick={() => setActiveTab("notes")}
              className={`flex items-center gap-1 pb-1.5 text-[10px] font-bold border-b-2 px-1 transition-all cursor-pointer ${
                activeTab === "notes"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground/50 hover:text-foreground"
              }`}
            >
              <Notebook className="h-3 w-3" />
              <span>Meeting Notes</span>
            </button>
          </div>

          {/* Active Tab Document Panel */}
          <div className="p-3 min-h-[110px] text-xs">
            {tabContents[activeTab]}
          </div>
        </div>

        {/* Project Update Box */}
        <div className="w-full p-2.5 border border-border/30 rounded-lg bg-card/25 hover:bg-card/45 cursor-pointer flex items-center justify-center gap-2 text-muted-foreground/75 hover:text-foreground transition-all duration-200 select-none group/update shadow-sm mt-1">
          <PenBox className="h-3.5 w-3.5 opacity-75 group-hover/update:scale-105 transition-transform" />
          <span className="text-[11px] font-bold">Write first project update</span>
        </div>

      </div>

      {/* 2. Right Column: Fixed Floating Sidebar (Displays Blocks styled like the image) */}
      <div className="w-[280px] shrink-0 flex flex-col gap-2 p-2 overflow-y-auto bg-transparent select-none [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-thumb]:bg-white/[0.05] [&::-webkit-scrollbar-thumb]:rounded-full">
        
        {/* Members Block */}
        <div className="p-3.5 rounded-xl border border-border/30 bg-[#161618] hover:bg-[#1b1b1d] transition-all duration-200 space-y-3 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground/80 flex items-center gap-1 cursor-pointer hover:text-foreground">
              Members <span className="text-[8px] text-muted-foreground/60">▶</span>
            </span>
            <button className="text-muted-foreground/60 hover:text-foreground text-xs font-medium cursor-pointer select-none">
              +
            </button>
          </div>
          
          <div className="space-y-2 text-xs">
            <div className="flex items-center gap-2 p-1.5 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <div className="h-5 w-5 rounded-full bg-cyan-500 border border-border/20 flex items-center justify-center text-[9px] font-black text-white shrink-0 shadow-sm">
                AD
              </div>
              <span className="font-bold text-[11px] text-foreground/85">Anh Đức Nguyễn</span>
              <span className="text-[8px] uppercase tracking-widest text-muted-foreground/45 ml-auto border border-border/25 px-1 py-0.5 rounded font-black">Owner</span>
            </div>
            <div className="flex items-center gap-2 p-1.5 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <div className="h-5 w-5 rounded-full bg-purple-500 border border-border/20 flex items-center justify-center text-[9px] font-black text-white shrink-0 shadow-sm">
                JD
              </div>
              <span className="font-bold text-[11px] text-foreground/85">John Doe</span>
              <span className="text-[8px] uppercase tracking-widest text-muted-foreground/45 ml-auto font-bold opacity-60">Admin</span>
            </div>
          </div>
        </div>

        {/* Workflow Block */}
        <div className="p-3.5 rounded-xl border border-border/30 bg-[#161618] hover:bg-[#1b1b1d] transition-all duration-200 space-y-3 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground/80 flex items-center gap-1 cursor-pointer hover:text-foreground">
              Workflow <span className="text-[8px] text-muted-foreground/60">▶</span>
            </span>
            <button className="text-muted-foreground/60 hover:text-foreground text-xs font-medium cursor-pointer select-none">
              +
            </button>
          </div>
          
          <div className="space-y-1.5">
            <div className="flex items-center justify-between p-1 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <StatusBadge status={{ name: "Backlog", color: "#6b7280", category: "NotStarted" } as any} />
              <span className="text-[9px] font-mono text-muted-foreground/40 font-bold">12 Tasks</span>
            </div>
            <div className="flex items-center justify-between p-1 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <StatusBadge status={{ name: "Planned", color: "#3b82f6", category: "NotStarted" } as any} />
              <span className="text-[9px] font-mono text-muted-foreground/40 font-bold">5 Tasks</span>
            </div>
            <div className="flex items-center justify-between p-1 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <StatusBadge status={{ name: "In Progress", color: "#f59e0b", category: "InProgress" } as any} />
              <span className="text-[9px] font-mono text-muted-foreground/40 font-bold">3 Tasks</span>
            </div>
            <div className="flex items-center justify-between p-1 rounded-md hover:bg-white/[0.02] transition-colors cursor-pointer">
              <StatusBadge status={{ name: "Completed", color: "#10b981", category: "Completed" } as any} />
              <span className="text-[9px] font-mono text-muted-foreground/40 font-bold">28 Tasks</span>
            </div>
          </div>
        </div>

      </div>

    </div>
  );
}
