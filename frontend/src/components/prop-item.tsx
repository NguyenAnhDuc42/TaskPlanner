import { type LucideIcon } from "lucide-react"

export function PropItem({ label, icon: Icon, value, onClick }: { label: string, icon: LucideIcon, value: string, onClick: () => void }) {
  return (
    <div 
      className="flex flex-col gap-1.5 group cursor-pointer transition-all hover:translate-x-1"
      onClick={onClick}
    >
      <label className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground/60">{label}</label>
      <div className="flex items-center gap-2 h-9 px-3 rounded-xl bg-background border border-border group-hover:border-primary/40 group-hover:bg-primary/5 transition-all">
        <Icon className="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary" />
        <span className="text-[11px] font-bold flex-1 truncate">{value}</span>
      </div>
    </div>
  )
}
