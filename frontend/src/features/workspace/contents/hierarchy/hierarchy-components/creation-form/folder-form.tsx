import * as React from "react"
import { 
  Folder, 
  CircleDashed,
} from "lucide-react"
import * as LucideIcons from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { UniversalPicker } from "@/components/universal-picker"
import { PropItem } from "@/components/prop-item"
import { useCreateFolder } from "../../hierarchy-api"

interface FolderFormProps {
  workspaceId: string
  spaceId: string
  onSubmitSuccess: () => void
  onCancel: () => void
}

export function FolderForm({ workspaceId, spaceId, onSubmitSuccess, onCancel }: FolderFormProps) {
  const createFolder = useCreateFolder(workspaceId)
  const [name, setName] = React.useState("")
  const [description, setDescription] = React.useState("")
  const [icon, setIcon] = React.useState("Folder")
  const [color, setColor] = React.useState("#10b981")
  const [status, setStatus] = React.useState("To Do")

  const SelectedIcon = (LucideIcons as any)[icon] || Folder

  return (
    <div className="flex flex-col w-[800px] bg-background overflow-hidden relative border-t border-border/50">
      <div className="flex flex-row h-[480px] w-full">
        {/* Main Area */}
        <div className="flex-1 min-w-0 flex flex-col p-6 gap-6 overflow-y-auto no-scrollbar">
          <div className="flex items-center gap-4">
            <Popover>
              <PopoverTrigger asChild>
                <button 
                  className="w-14 h-14 rounded-2xl flex-shrink-0 flex items-center justify-center transition-all hover:scale-105 active:scale-95 shadow-md border"
                  style={{ backgroundColor: `${color}15`, color: color, borderColor: `${color}30` }}
                >
                  <SelectedIcon className="h-7 w-7" />
                </button>
              </PopoverTrigger>
              <PopoverContent side="right" className="p-0 border-none shadow-xl" align="start">
                <UniversalPicker 
                  selectedIcon={icon} 
                  selectedColor={color} 
                  onSelect={(i, c) => { setIcon(i); setColor(c); }} 
                />
              </PopoverContent>
            </Popover>

            <div className="flex-1 min-w-0">
              <Input 
                placeholder="Name your folder..." 
                className="h-14 px-4 text-xl font-bold border-none bg-muted/20 hover:bg-muted/30 focus-visible:bg-muted/40 transition-all rounded-2xl"
                value={name}
                onChange={(e) => setName(e.target.value)}
                autoFocus
                onKeyDown={(e) => e.key === 'Enter' && name.trim() && createFolder.mutate({ ...{ name, description, icon, color }, spaceId }, { onSuccess: onSubmitSuccess })}
              />
            </div>
          </div>

          <div className="flex-1 flex flex-col gap-2">
            <label className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground pl-1">Description</label>
            <Textarea 
              placeholder="What's inside this folder? Add instructions or context..." 
              className="flex-1 min-h-[120px] resize-none bg-muted/10 border-none rounded-2xl p-4 text-sm leading-relaxed focus-visible:ring-1 focus-visible:ring-primary/20"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
        </div>

        {/* Props Sidebar */}
        <div className="w-[280px] flex-shrink-0 border-l border-border bg-muted/5 p-6 flex flex-col gap-6 overflow-y-auto no-scrollbar">
           <div className="flex flex-col gap-1">
              <span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground opacity-50">Folder Settings</span>
           </div>
           <div className="flex flex-col gap-4">
             <PropItem 
               label="Status" 
               icon={CircleDashed} 
               value={status} 
               onClick={() => setStatus(status === "To Do" ? "In Progress" : status === "In Progress" ? "Done" : "To Do")} 
             />
           </div>
        </div>
      </div>

      <div className="px-6 py-4 bg-muted/10 border-t border-border/50 flex items-center justify-between flex-row">
          <Button variant="ghost" onClick={onCancel} className="rounded-xl font-bold uppercase tracking-widest text-[11px] h-10 px-6">
            Cancel
          </Button>
          <Button 
              onClick={() => createFolder.mutate({ ...{ name, description, icon, color }, spaceId }, { onSuccess: onSubmitSuccess })} 
              disabled={!name.trim() || createFolder.isPending}
              className="rounded-xl font-bold uppercase tracking-widest text-[11px] px-10 h-10 shadow-lg shadow-primary/10"
          >
            {createFolder.isPending ? "Creating..." : "Create Folder"}
          </Button>
      </div>
    </div>
  )
}
