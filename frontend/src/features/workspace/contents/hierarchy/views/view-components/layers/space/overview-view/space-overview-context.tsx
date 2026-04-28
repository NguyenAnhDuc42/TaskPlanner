import { ScrollArea } from "@/components/ui/scroll-area";
import {
  MessageSquare,
  Users,
  GitMerge,
  FileText,
  User,
  Calendar,
  Layers,
  ChevronDown,
  Layout
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export function SpaceOverviewContext() {
  return (
    <div className="flex-1 flex flex-col h-full bg-background select-none">
      <Tabs defaultValue="general" className="flex-1 flex flex-col h-full">
        {/* TAB HEADER - CLEANED UP TRIGGERS */}
        <div className="h-12 px-5 flex items-center justify-between border-b border-border/40 bg-background/50 backdrop-blur-md">
          <TabsList className="bg-transparent border-none p-0 h-full gap-3 items-center">
            <TabIconTrigger value="general" icon={Layout} />
            <TabIconTrigger value="docs" icon={FileText} />
            <TabIconTrigger value="members" icon={Users} />
          </TabsList>
          
        </div>

        <ScrollArea className="flex-1">
          <TabsContent value="general" className="m-0 animate-in fade-in slide-in-from-right-1 duration-200">
            
            {/* PROPERTY GRID - LINEAR STYLE */}
            <div className="py-4 space-y-1">
              <div className="px-5 pb-2">
                 <span className="text-[10px] font-bold uppercase tracking-[0.2em] text-muted-foreground/30">Properties</span>
              </div>
              
              <PropertyRow icon={Layers} label="Status">
                <div className="flex items-center gap-2">
                   <div className="h-2 w-2 rounded-full bg-emerald-500" />
                   <span className="font-semibold text-foreground/80">Active Session</span>
                </div>
              </PropertyRow>

              <PropertyRow icon={GitMerge} label="Workflow">
                 <span className="font-semibold text-foreground/80">Default Engine</span>
              </PropertyRow>

              <PropertyRow icon={User} label="Owner">
                 <div className="flex items-center gap-2">
                    <div className="h-4 w-4 rounded-full bg-primary/10 flex items-center justify-center text-[8px] font-bold text-primary border border-primary/20">A</div>
                    <span className="font-semibold text-foreground/80">Admin</span>
                 </div>
              </PropertyRow>

              <PropertyRow icon={Calendar} label="Created">
                 <span className="font-semibold text-foreground/80">Oct 24, 2024</span>
              </PropertyRow>

              <PropertyRow icon={MessageSquare} label="Channel">
                 <span className="font-semibold text-primary hover:underline cursor-pointer">#general-space</span>
              </PropertyRow>
            </div>

            {/* SECTIONS - LINEAR STYLE */}
            <div className="mt-4 border-t border-border/40">
               <CollapsibleSection title="Milestones" count={0}>
                  <p className="text-[11px] text-muted-foreground/50 leading-relaxed">
                    No active milestones. Create one to track major progress points for this space.
                  </p>
               </CollapsibleSection>

               <CollapsibleSection title="Progress" count={null}>
                  <div className="space-y-4">
                     <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                           <div className="w-2 h-2 rounded-sm bg-muted" />
                           <span className="text-[11px] font-bold text-muted-foreground/60">Scope</span>
                           <span className="text-[11px] font-black">12</span>
                        </div>
                        <div className="flex items-center gap-3">
                           <div className="w-2 h-2 rounded-sm bg-primary" />
                           <span className="text-[11px] font-bold text-muted-foreground/60">Done</span>
                           <span className="text-[11px] font-black text-primary">8</span>
                        </div>
                     </div>
                     {/* Mini Progress Bar */}
                     <div className="h-1.5 w-full bg-muted rounded-full overflow-hidden">
                        <div className="h-full bg-primary w-[66%] transition-all" />
                     </div>
                  </div>
               </CollapsibleSection>
            </div>

          </TabsContent>

          <TabsContent value="docs" className="m-0 p-6 animate-in fade-in slide-in-from-right-1 duration-200">
             <div className="flex flex-col items-center justify-center py-12 text-center opacity-30">
                <FileText className="h-8 w-8 mb-4 stroke-[1.2]" />
                <span className="text-[11px] font-bold uppercase tracking-widest">No Linked Docs</span>
             </div>
          </TabsContent>

          <TabsContent value="members" className="m-0 p-6 animate-in fade-in slide-in-from-right-1 duration-200">
             <div className="space-y-4">
                {[1, 2, 3].map((i) => (
                   <div key={i} className="flex items-center justify-between group">
                      <div className="flex items-center gap-3">
                         <div className="h-7 w-7 rounded-full bg-foreground/5 flex items-center justify-center border border-border/40 text-[9px] font-bold">U{i}</div>
                         <span className="text-xs font-bold text-foreground/80">Collaborator {i}</span>
                      </div>
                      <div className="h-1.5 w-1.5 rounded-full bg-emerald-500 opacity-40" />
                   </div>
                ))}
             </div>
          </TabsContent>
        </ScrollArea>
      </Tabs>
    </div>
  );
}

function TabIconTrigger({ value, icon: Icon }: { value: string, icon: any }) {
   return (
      <TabsTrigger
        value={value}
        className="h-full px-0 bg-transparent shadow-none border-none data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:text-primary text-muted-foreground/50 hover:text-foreground transition-all relative group focus-visible:ring-0 focus-visible:ring-offset-0"
      >
        <Icon className="h-4 w-4" />
        <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-primary rounded-t-full scale-x-0 group-data-[state=active]:scale-x-100 transition-transform origin-center" />
      </TabsTrigger>
   );
}

function PropertyRow({ icon: Icon, label, children }: { icon: any, label: string, children: React.ReactNode }) {
  return (
    <div className="grid grid-cols-[110px_1fr] items-center px-5 py-1.5 hover:bg-foreground/[0.02] transition-colors group">
      <div className="flex items-center gap-2.5 text-muted-foreground/50 group-hover:text-muted-foreground transition-colors">
         <Icon className="h-3.5 w-3.5" />
         <span className="text-[11px] font-bold tracking-tight">{label}</span>
      </div>
      <div className="text-[12px]">
         {children}
      </div>
    </div>
  );
}

function CollapsibleSection({ title, count, children }: { title: string, count: number | null, children: React.ReactNode }) {
   return (
      <div className="border-b border-border/40">
         <div className="flex items-center justify-between px-5 py-2.5 cursor-pointer hover:bg-foreground/[0.02] transition-colors group">
            <div className="flex items-center gap-2">
               <ChevronDown className="h-3 w-3 text-muted-foreground/30 group-hover:text-muted-foreground transition-colors" />
               <span className="text-[11px] font-black uppercase tracking-widest text-foreground/70">{title}</span>
            </div>
            {count !== null && (
               <span className="text-[10px] font-black text-muted-foreground/20">{count}</span>
            )}
         </div>
         <div className="px-5 pb-4 pt-1">
            {children}
         </div>
      </div>
   );
}
