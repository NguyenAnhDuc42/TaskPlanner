import * as React from "react"
import { Search, LayoutGrid } from "lucide-react"
import * as LucideIcons from "lucide-react"
import { cn } from "@/lib/utils"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"

const PRESET_COLORS = [
  "#94a3b8", // Slate
  "#64748b", // Slate Dark
  "#6366f1", // Indigo
  "#06b6d4", // Cyan
  "#10b981", // Emerald
  "#f59e0b", // Amber
  "#f97316", // Orange
  "#fca5a5", // Red Light
  "#ef4444", // Red
  "#ec4899", // Pink
]

const POPULAR_ICONS = [
  "LayoutGrid", "Folder", "FileText", "CheckSquare", "Calendar", "Clock", "User", "Users", 
  "Settings", "Bell", "Inbox", "Hash", "Star", "Heart", "Flag", "Bookmark", "Tag", 
  "Search", "Zap", "Flame", "Target", "Trophy", "Gamepad2", "Code2", "Terminal", 
  "Layers", "Component", "Box", "Package", "Database", "Cloud", "HardDrive", "Cpu", 
  "Smartphone", "Laptop", "Monitor", "Headphones", "Camera", "Mic", "Music", "Video", 
  "Image", "Paintbrush", "PenTool", "Brush", "Eraser", "Scissors", "Link", "Paperclip", 
  "Mail", "Send", "Share2", "ExternalLink", "Download", "Upload", "Globe", "Map", 
  "Compass", "Navigation", "Sun", "Moon", "Wind", "Umbrella", 
  "ShoppingBag", "ShoppingCart", "CreditCard", "Briefcase", "Building", "Home", 
  "Key", "Lock", "Unlock", "Shield", "Eye", "EyeOff", "Trash2", "Archive", 
  "Plus", "Minus", "X", "Check", "ChevronRight", "ChevronDown", "MoreHorizontal"
]

interface UniversalPickerProps {
  selectedIcon: string
  selectedColor: string
  onSelect: (icon: string, color: string) => void
}

export function UniversalPicker({ selectedIcon, selectedColor, onSelect }: UniversalPickerProps) {
  const [searchQuery, setSearchQuery] = React.useState("")
  
  const filteredIcons = React.useMemo(() => {
    return POPULAR_ICONS.filter(name => 
      name.toLowerCase().includes(searchQuery.toLowerCase())
    )
  }, [searchQuery])

  return (
    <div className="w-80 flex flex-col gap-4 p-3 bg-background border border-border shadow-xl rounded-xl" onClick={(e) => e.stopPropagation()}>
      <Tabs defaultValue="icons" className="w-full">
        <TabsList className="grid w-full grid-cols-2 p-1 bg-muted/50 h-9 rounded-lg">
          <TabsTrigger value="icons" className="text-[11px] font-bold uppercase tracking-wider rounded-md data-[state=active]:bg-background data-[state=active]:shadow-sm">Icons</TabsTrigger>
          <TabsTrigger value="emojis" className="text-[11px] font-bold uppercase tracking-wider rounded-md data-[state=active]:bg-background data-[state=active]:shadow-sm">Emojis</TabsTrigger>
        </TabsList>
        
        <div className="mt-4 flex flex-col gap-4">
          {/* Color Bar */}
          <div className="flex items-center gap-1.5 overflow-x-auto pb-1 no-scrollbar">
            {PRESET_COLORS.map(color => (
              <button
                key={color}
                className={cn(
                  "w-6 h-6 rounded-full flex-shrink-0 transition-all hover:scale-110 active:scale-95",
                  selectedColor === color ? "ring-2 ring-primary ring-offset-2 ring-offset-background" : "ring-1 ring-border"
                )}
                style={{ backgroundColor: color }}
                onClick={() => onSelect(selectedIcon, color)}
              />
            ))}
          </div>

          <TabsContent value="icons" className="mt-0 flex flex-col gap-3">
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input 
                placeholder="Search icons..." 
                className="pl-8 h-9 text-[11px] bg-muted/30 border-none rounded-lg"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>

            <ScrollArea className="h-48 rounded-lg border border-border/50 bg-muted/10">
              <div className="grid grid-cols-6 gap-1 p-2">
                {filteredIcons.map(name => {
                  const Icon = (LucideIcons as any)[name] || LayoutGrid
                  const isActive = selectedIcon === name
                  return (
                    <button
                      key={name}
                      className={cn(
                        "w-10 h-10 flex items-center justify-center rounded-lg transition-all",
                        isActive ? "bg-primary/20 text-primary" : "hover:bg-muted text-muted-foreground hover:text-foreground"
                      )}
                      onClick={() => onSelect(name, selectedColor)}
                      title={name}
                    >
                      <Icon className="h-5 w-5" />
                    </button>
                  )
                })}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="emojis" className="mt-0 h-60 flex items-center justify-center text-muted-foreground text-[11px] font-bold uppercase tracking-widest italic opacity-50">
            Emojis coming soon
          </TabsContent>
        </div>
      </Tabs>
    </div>
  )
}
