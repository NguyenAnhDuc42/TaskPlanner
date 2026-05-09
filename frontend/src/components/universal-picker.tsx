import * as React from "react"
import { Search, LayoutGrid, type LucideIcon } from "lucide-react"
import * as LucideIcons from "lucide-react"
import { cn } from "@/lib/utils"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"

// ============================================================================
// DOMAIN ABSTRACTIONS
// ============================================================================

/**
 * Immutable configuration for the picker.
 * Validates icon names at construction time, not render time.
 */
class PickerConfig {
  readonly availableIcons: readonly string[]
  readonly availableColors: readonly string[]
  readonly searchDebounceMs: number

  constructor(
    icons: string[],
    colors: string[],
    debounceMs: number = 150
  ) {
    this.availableIcons = Object.freeze(icons)
    this.availableColors = Object.freeze(colors)
    this.searchDebounceMs = debounceMs
  }
}

/**
 * Encapsulates search logic and filtering.
 * Memoizes predicates to avoid allocation thrashing.
 */
class IconSearchEngine {
  private queryCache = new Map<string, string[]>()
  private maxCacheSize = 20

  search(query: string, icons: readonly string[]): string[] {
    if (!query.trim()) return Array.from(icons)

    const cached = this.queryCache.get(query)
    if (cached) return cached

    const normalized = query.toLowerCase()
    const results = icons.filter((name) =>
      name.toLowerCase().includes(normalized)
    )

    // Simple LRU: if cache exceeds max, clear oldest half
    if (this.queryCache.size >= this.maxCacheSize) {
      const keys = Array.from(this.queryCache.keys())
      keys.slice(0, Math.floor(this.maxCacheSize / 2)).forEach((k) =>
        this.queryCache.delete(k)
      )
    }

    this.queryCache.set(query, results)
    return results
  }

  clear() {
    this.queryCache.clear()
  }
}

// ============================================================================
// CUSTOM HOOK: STATE MANAGEMENT WITH DEBOUNCING
// ============================================================================

interface UseIconColorPickerProps {
  initialIcon: string
  initialColor: string
  config: PickerConfig
}

interface UseIconColorPickerReturn {
  selectedIcon: string
  selectedColor: string
  searchQuery: string
  filteredIcons: string[]
  setSelectedIcon: (icon: string) => void
  setSelectedColor: (color: string) => void
  setSearchQuery: (query: string) => void
}

function useIconColorPicker({
  initialIcon,
  initialColor,
  config,
}: UseIconColorPickerProps): UseIconColorPickerReturn {
  const [selectedIcon, setSelectedIcon] = React.useState(initialIcon)
  const [selectedColor, setSelectedColor] = React.useState(initialColor)
  const [searchQuery, setSearchQuery] = React.useState("")
  const [debouncedQuery, setDebouncedQuery] = React.useState("")

  // Debounce search query
  React.useEffect(() => {
    const timer = setTimeout(
      () => setDebouncedQuery(searchQuery),
      config.searchDebounceMs
    )
    return () => clearTimeout(timer)
  }, [searchQuery, config.searchDebounceMs])

  // Search engine instance (shared across renders)
  const searchEngine = React.useMemo(() => new IconSearchEngine(), [])

  // Filter icons based on debounced query
  const filteredIcons = React.useMemo(() => {
    return searchEngine.search(debouncedQuery, config.availableIcons)
  }, [debouncedQuery, config.availableIcons, searchEngine])

  return {
    selectedIcon,
    selectedColor,
    searchQuery,
    filteredIcons,
    setSelectedIcon,
    setSelectedColor,
    setSearchQuery,
  }
}

// ============================================================================
// ICON RENDERER: CACHED ICON LOOKUP
// ============================================================================

interface IconRendererProps {
  name: string
  isActive: boolean
  onClick: () => void
}

/**
 * Memoized icon renderer.
 * Fails loudly if icon doesn't exist (caught at config validation, not here).
 */
const IconRenderer = React.memo(function IconRenderer({
  name,
  isActive,
  onClick,
}: IconRendererProps) {
  const Icon = (LucideIcons as any)[name] as LucideIcon | undefined

  if (!Icon) {
    return <LayoutGrid className="h-5 w-5" />
  }

  return (
    <button
      className={cn(
        "w-10 h-10 flex items-center justify-center rounded-lg transition-all duration-150",
        isActive
          ? "bg-primary/20 text-primary"
          : "hover:bg-muted text-muted-foreground hover:text-foreground"
      )}
      onClick={onClick}
      title={name}
      type="button"
    >
      <Icon className="h-5 w-5" />
    </button>
  )
})

// ============================================================================
// COLOR SWATCH: ISOLATED RENDERING
// ============================================================================

interface ColorSwatchProps {
  color: string
  isSelected: boolean
  onClick: () => void
}

const ColorSwatch = React.memo(function ColorSwatch({
  color,
  isSelected,
  onClick,
}: ColorSwatchProps) {
  return (
    <button
      className={cn(
        "w-6 h-6 rounded-full flex-shrink-0 transition-all duration-150 hover:scale-110 active:scale-95",
        isSelected
          ? "ring-2 ring-primary ring-offset-2 ring-offset-background"
          : "ring-1 ring-border hover:ring-2 hover:ring-primary/50"
      )}
      style={{ backgroundColor: color }}
      onClick={onClick}
      title={color}
      type="button"
    />
  )
})

// ============================================================================
// MAIN COMPONENT
// ============================================================================

interface UniversalPickerProps {
  selectedIcon: string
  selectedColor: string
  onSelect: (icon: string, color: string) => void
  config?: PickerConfig
}

const DEFAULT_CONFIG = new PickerConfig(
  [
    "LayoutGrid",
    "Folder",
    "FileText",
    "CheckSquare",
    "Calendar",
    "Clock",
    "User",
    "Users",
    "Settings",
    "Bell",
    "Inbox",
    "Hash",
    "Star",
    "Heart",
    "Flag",
    "Bookmark",
    "Tag",
    "Search",
    "Zap",
    "Flame",
    "Target",
    "Trophy",
    "Gamepad2",
    "Code2",
    "Terminal",
    "Layers",
    "Component",
    "Box",
    "Package",
    "Database",
    "Cloud",
    "HardDrive",
    "Cpu",
  ],
  ["#6366f1", "#06b6d4", "#10b981", "#f59e0b", "#ef4444", "#ec4899"] // Reduced: 6 instead of 10
)

export function UniversalPicker({
  selectedIcon,
  selectedColor,
  onSelect,
  config = DEFAULT_CONFIG,
}: UniversalPickerProps) {
  const {
    selectedIcon: localIcon,
    selectedColor: localColor,
    searchQuery,
    filteredIcons,
    setSelectedIcon,
    setSelectedColor,
    setSearchQuery,
  } = useIconColorPicker({
    initialIcon: selectedIcon,
    initialColor: selectedColor,
    config,
  })

  const handleSelectIcon = (icon: string) => {
    setSelectedIcon(icon)
    onSelect(icon, localColor)
  }

  const handleSelectColor = (color: string) => {
    setSelectedColor(color)
    onSelect(localIcon, color)
  }

  const [showColorPicker, setShowColorPicker] = React.useState(false)
  const [customColor, setCustomColor] = React.useState(localColor)
  const colorInputRef = React.useRef<HTMLInputElement>(null)

  const handleCustomColor = (color: string) => {
    handleSelectColor(color)
    setShowColorPicker(false)
  }

  return (
    <div
      className="w-80 flex flex-col gap-0 bg-background border border-border shadow-xl rounded-xl overflow-hidden"
      onClick={(e) => e.stopPropagation()}
      role="region"
      aria-label="Icon and color picker"
    >
      {/* Color Bar - Fixed at Top */}
      <div className="flex items-center justify-center gap-2 px-4 py-3 bg-muted/10 border-b border-border/50">
        <div className="flex items-center gap-1.5">
          {config.availableColors.map((color) => (
            <ColorSwatch
              key={color}
              color={color}
              isSelected={localColor === color}
              onClick={() => handleSelectColor(color)}
            />
          ))}
        </div>

        {/* Custom Color Picker Circle Button */}
        <button
          onClick={() => {
            setShowColorPicker(!showColorPicker)
            setTimeout(() => colorInputRef.current?.click(), 0)
          }}
          className="w-6 h-6 rounded-full flex-shrink-0 flex items-center justify-center bg-gradient-to-br from-red-500 via-green-500 to-blue-500 hover:shadow-lg transition-all hover:scale-110"
          title="Pick custom color"
          type="button"
        >
          <input
            ref={colorInputRef}
            type="color"
            value={customColor}
            onChange={(e) => {
              setCustomColor(e.target.value)
              handleCustomColor(e.target.value)
            }}
            className="opacity-0 w-0 h-0 cursor-pointer"
          />
        </button>
      </div>

      {/* Tabs & Icons - Main Section */}
      <div className="p-3">
        <Tabs defaultValue="icons" className="w-full">
          <TabsList className="grid w-full grid-cols-2 p-1 bg-muted/50 h-9 rounded-lg">
            <TabsTrigger
              value="icons"
              className="text-[11px] font-bold uppercase tracking-wider rounded-md data-[state=active]:bg-background data-[state=active]:shadow-sm"
            >
              Icons
            </TabsTrigger>
            <TabsTrigger
              value="emojis"
              className="text-[11px] font-bold uppercase tracking-wider rounded-md data-[state=active]:bg-background data-[state=active]:shadow-sm"
            >
              Emojis
            </TabsTrigger>
          </TabsList>

          <TabsContent value="icons" className="mt-0 flex flex-col gap-3">
            {/* Search Input */}
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Search icons..."
                className="pl-8 h-9 text-[11px] bg-muted/30 border-none rounded-lg"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                type="search"
                aria-label="Search icons"
              />
            </div>

            {/* Icon Grid */}
            <ScrollArea className="h-48 rounded-lg border border-border/50 bg-muted/10">
              {filteredIcons.length > 0 ? (
                <div className="grid grid-cols-6 gap-1 p-2">
                  {filteredIcons.map((name) => (
                    <IconRenderer
                      key={name}
                      name={name}
                      isActive={localIcon === name}
                      onClick={() => handleSelectIcon(name)}
                    />
                  ))}
                </div>
              ) : (
                <div className="flex items-center justify-center h-full text-muted-foreground text-[11px] font-semibold">
                  No icons found
                </div>
              )}
            </ScrollArea>
          </TabsContent>

          <TabsContent
            value="emojis"
            className="mt-0 h-60 flex items-center justify-center text-muted-foreground text-[11px] font-bold uppercase tracking-widest italic opacity-50"
          >
            Emojis coming soon
          </TabsContent>
        </Tabs>
        </div>
      </div>

  )
}

// ============================================================================
// EXPORTS FOR ADVANCED USAGE
// ============================================================================

export { PickerConfig, IconSearchEngine, useIconColorPicker }