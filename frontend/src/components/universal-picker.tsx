import * as React from "react"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { DynamicIcon } from "@/components/dynamic-icon"
import { cn } from "@/lib/utils"
import {
  LayoutGrid, Folder, FileText, CheckSquare, Calendar, Clock, User, Users,
  Settings, Bell, Inbox, Hash, Star, Heart, Flag, Bookmark, Tag, Search,
  Zap, Flame, Target, Trophy, Gamepad2, Code2, Terminal, Layers, Component,
  Box, Package, Database, Cloud, HardDrive, Cpu, BarChart2, PieChart, TrendingUp,
  Lock, Unlock, Shield, Eye, EyeOff, Link, Mail, Phone, MapPin, Home,
  Briefcase, Clipboard, List, AlignLeft, Image, Video, Music, Mic, Headphones,
  Monitor, Tablet, Smartphone, Printer, Camera, Globe, Wifi, Bluetooth,
  Battery, Sun, Moon, Wind, Droplets, Leaf, Trees, Bug, Rocket, Anchor,
} from "lucide-react"

const ICON_MAP: Record<string, React.ElementType> = {
  LayoutGrid, Folder, FileText, CheckSquare, Calendar, Clock, User, Users,
  Settings, Bell, Inbox, Hash, Star, Heart, Flag, Bookmark, Tag, Search,
  Zap, Flame, Target, Trophy, Gamepad2, Code2, Terminal, Layers, Component,
  Box, Package, Database, Cloud, HardDrive, Cpu, BarChart2, PieChart, TrendingUp,
  Lock, Unlock, Shield, Eye, EyeOff, Link, Mail, Phone, MapPin, Home,
  Briefcase, Clipboard, List, AlignLeft, Image, Video, Music, Mic, Headphones,
  Monitor, Tablet, Smartphone, Printer, Camera, Globe, Wifi, Bluetooth,
  Battery, Sun, Moon, Wind, Droplets, Leaf, Trees, Bug, Rocket, Anchor,
}

const AVAILABLE_ICONS = Object.keys(ICON_MAP)

const EMOJIS = [
  "😀","😃","😄","😁","😆","🤣","😂","😊","😇","🥰",
  "😍","🤩","🥳","😎","🤓","🧐","🤔","😤","😠","🤬",
  "❤️","🧡","💛","💚","💙","💜","🖤","🤍","💯","✅",
  "❌","⭐","🌟","💫","✨","🔥","⚡","💥","🎯","🚀",
  "🏆","🥇","🎖️","🎉","🎊","🎈","🎁","🏅","🏵️","🎗️",
  "💡","🔑","🔒","🛠️","⚙️","🔧","🔔","💬","📌","📎",
  "📁","📂","🗂️","📊","📈","📉","📝","✏️","📋","📅",
  "💼","🧳","👜","📦","📫","📧","💌","📱","💻","🖥️",
  "🎨","🎭","🎵","🎶","🎮","🕹️","🎲","♟️","🧩","🃏",
  "🌈","🌊","🌙","☀️","⛅","🌍","🌿","🍀","🌸","🌺",
]

const DEFAULT_COLORS = [
  "#6366f1", "#06b6d4", "#10b981", "#f59e0b", "#ef4444", "#ec4899",
  "#8b5cf6", "#14b8a6", "#f97316", "#84cc16",
]

const SIZE = {
  sm: { button: "p-1 rounded-md border border-border/15 bg-background/80 hover:bg-muted/60 shadow-sm", iconSize: 12 },
  md: { button: "h-6 w-6 rounded-md bg-muted/20 hover:bg-muted/50 flex items-center justify-center", iconSize: 16 },
  lg: { button: "h-9 w-9 rounded-md border border-border/50 bg-muted/20 hover:bg-muted/40 flex items-center justify-center", iconSize: 20 },
} as const

const IconButton = React.memo(function IconButton({
  name, isActive, activeColor, onClick,
}: { name: string; isActive: boolean; activeColor: string; onClick: () => void }) {
  const Icon = ICON_MAP[name]
  if (!Icon) return null
  return (
    <button
      type="button"
      title={name}
      onClick={onClick}
      className={cn(
        "w-11 h-11 flex items-center justify-center rounded-md transition-all",
        isActive
          ? "bg-muted shadow-sm ring-1 ring-border"
          : "text-muted-foreground hover:text-foreground hover:bg-muted/60",
      )}
      style={isActive ? { color: activeColor } : undefined}
    >
      <Icon className="h-6 w-6" />
    </button>
  )
})

const ColorSwatch = React.memo(function ColorSwatch({
  color, isSelected, onClick,
}: { color: string; isSelected: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      title={color}
      onClick={onClick}
      className={cn(
        "w-5 h-5 rounded-full transition-all hover:scale-110 active:scale-95",
        isSelected
          ? "ring-2 ring-offset-1 ring-offset-background ring-foreground/40 scale-110"
          : "ring-1 ring-border hover:ring-foreground/20",
      )}
      style={{ backgroundColor: color }}
    />
  )
})

export function PickerPanel({
  icon, color, colors, onSelect,
}: {
  icon: string
  color: string
  colors: string[]
  onSelect: (icon: string, color: string) => void
}) {
  const [localIcon, setLocalIcon] = React.useState(icon)
  const [localColor, setLocalColor] = React.useState(color)
  const [search, setSearch] = React.useState("")
  const [tab, setTab] = React.useState<"icons" | "emojis">("icons")
  const [hexInput, setHexInput] = React.useState(color)

  const [prevIcon, setPrevIcon] = React.useState(icon)
  if (icon !== prevIcon) {
    setPrevIcon(icon)
    setLocalIcon(icon)
  }
  const [prevColor, setPrevColor] = React.useState(color)
  if (color !== prevColor) {
    setPrevColor(color)
    setLocalColor(color)
    setHexInput(color)
  }

  const filteredIcons = React.useMemo(() => {
    if (!search.trim()) return AVAILABLE_ICONS
    const q = search.toLowerCase()
    return AVAILABLE_ICONS.filter((name) => name.toLowerCase().includes(q))
  }, [search])

  const filteredEmojis = React.useMemo(() => {
    if (!search.trim()) return EMOJIS
    return EMOJIS // emoji search not meaningful, show all
  }, [search])

  const selectIcon = (i: string) => { setLocalIcon(i); onSelect(i, localColor) }
  const selectColor = (c: string) => { setLocalColor(c); setHexInput(c); onSelect(localIcon, c) }

  const handleHexChange = (val: string) => {
    // Auto-prepend # if user types without it
    const normalized = val.startsWith("#") ? val : `#${val}`
    setHexInput(normalized)
    if (/^#[0-9a-fA-F]{6}$/.test(normalized)) selectColor(normalized)
  }

  return (
    <div className="w-80 flex flex-col bg-popover border border-border shadow-md rounded-md overflow-hidden">
      {/* Color row */}
      <div className="flex items-center gap-2 px-3 py-2.5 border-b border-border/50">
        <span className="text-[9px] font-black uppercase tracking-widest text-muted-foreground/50 shrink-0">
          Color
        </span>
        <div className="flex items-center gap-1.5 flex-wrap flex-1">
          {colors.map((c) => (
            <ColorSwatch key={c} color={c} isSelected={localColor === c} onClick={() => selectColor(c)} />
          ))}
        </div>
        <div className="flex items-center gap-1 shrink-0">
          <label
            title="Custom color"
            className="relative w-4 h-4 rounded-full ring-1 ring-border shrink-0 cursor-pointer overflow-hidden"
            style={{ backgroundColor: localColor }}
          >
            <input
              type="color"
              value={/^#[0-9a-fA-F]{6}$/.test(localColor) ? localColor : "#000000"}
              onChange={(e) => selectColor(e.target.value)}
              onPointerDown={(e) => e.stopPropagation()}
              className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
            />
          </label>
          <input
            type="text"
            value={hexInput}
            maxLength={7}
            placeholder="#000000"
            onChange={(e) => handleHexChange(e.target.value)}
            onPointerDown={(e) => e.stopPropagation()}
            className="w-16 h-5 text-[9px] px-1.5 rounded border border-border/30 bg-muted/30 font-mono outline-none focus:border-primary/50 text-foreground/70"
          />
        </div>
      </div>

      {/* Tab bar */}
      <div className="flex border-b border-border/50">
        {(["icons", "emojis"] as const).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            className={cn(
              "flex-1 py-1.5 text-[10px] font-black uppercase tracking-wider transition-colors",
              tab === t
                ? "text-foreground border-b-2 border-primary"
                : "text-muted-foreground/50 hover:text-muted-foreground",
            )}
          >
            {t}
          </button>
        ))}
      </div>

      {tab === "icons" ? (
        <div className="flex flex-col">
          <div className="px-2 pt-2 pb-1">
            <div className="relative">
              <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3 w-3 text-muted-foreground/40" />
              <input
                placeholder="Search icons..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onPointerDown={(e) => e.stopPropagation()}
                className="h-7 w-full pl-6 pr-2 text-[11px] bg-muted/30 border border-border/40 rounded-md outline-none focus:border-primary/50 text-foreground/80 placeholder:text-muted-foreground/40"
              />
            </div>
          </div>
          <ScrollArea className="h-48">
            {filteredIcons.length > 0 ? (
              <div className="grid grid-cols-5 gap-0.5 p-2">
                {filteredIcons.map((name) => (
                  <IconButton
                    key={name}
                    name={name}
                    isActive={localIcon === name}
                    activeColor={localColor}
                    onClick={() => selectIcon(name)}
                  />
                ))}
              </div>
            ) : (
              <div className="flex items-center justify-center h-full text-[11px] text-muted-foreground/50 py-8">
                No icons found
              </div>
            )}
          </ScrollArea>
        </div>
      ) : (
        <ScrollArea className="h-56">
          <div className="grid grid-cols-7 gap-0.5 p-2">
            {filteredEmojis.map((emoji, i) => (
              <button
                key={i}
                type="button"
                onClick={() => selectIcon(emoji)}
                className={cn(
                  "w-10 h-10 flex items-center justify-center rounded-md text-xl transition-all hover:bg-muted/60",
                  localIcon === emoji && "bg-muted ring-1 ring-border"
                )}
              >
                {emoji}
              </button>
            ))}
          </div>
        </ScrollArea>
      )}
    </div>
  )
}

interface UniversalPickerProps {
  icon: string
  color: string
  onSelect: (icon: string, color: string) => void
  size?: keyof typeof SIZE
  align?: "start" | "end" | "center"
  colors?: string[]
}

export function UniversalPicker({
  icon,
  color,
  onSelect,
  size = "md",
  align = "start",
  colors = DEFAULT_COLORS,
}: Readonly<UniversalPickerProps>) {
  const { button, iconSize } = SIZE[size]

  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn("cursor-pointer transition-colors shrink-0 focus:outline-none", button)}
          style={{ color }}
        >
          <DynamicIcon name={icon || "LayoutGrid"} size={iconSize} color={color} />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align={align}
        sideOffset={6}
        className="w-fit p-0 border-none shadow-none bg-transparent"
      >
        <PickerPanel icon={icon} color={color} colors={colors} onSelect={onSelect} />
      </PopoverContent>
    </Popover>
  )
}
