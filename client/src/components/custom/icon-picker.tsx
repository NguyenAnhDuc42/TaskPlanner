import { useState } from "react";

import {
  Building2,
  Users,
  Briefcase,
  Code,
  Palette,
  Zap,
  Target,
  Heart,
  Star,
  Coffee,
  Lightbulb,
  Rocket,
  Shield,
  Globe,
  Camera,
  Music,
  Book,
  Calendar,
  Mail,
  Phone,
  Settings,
  Home,
  Car,
  Plane,
  MapPin,
  Clock,
  CheckCircle,
  AlertCircle,
  Info,
  Plus,
  Minus,
  X,
  Check,
  Search,
  Filter,
  Download,
  Upload,
  Edit,
  Trash2,
  Copy,
  Share,
  Eye,
  EyeOff,
  Lock,
  Unlock,
  ChevronRight,
  ChevronLeft,
  ChevronUp,
  ChevronDown
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "../ui/popover";
import { Button } from "../ui/button";
import { ScrollArea } from "../ui/scroll-area";

const availableIcons = [
  { name: "Building2", icon: Building2 },
  { name: "Users", icon: Users },
  { name: "Briefcase", icon: Briefcase },
  { name: "Code", icon: Code },
  { name: "Palette", icon: Palette },
  { name: "Zap", icon: Zap },
  { name: "Target", icon: Target },
  { name: "Heart", icon: Heart },
  { name: "Star", icon: Star },
  { name: "Coffee", icon: Coffee },
  { name: "Lightbulb", icon: Lightbulb },
  { name: "Rocket", icon: Rocket },
  { name: "Shield", icon: Shield },
  { name: "Globe", icon: Globe },
  { name: "Camera", icon: Camera },
  { name: "Music", icon: Music },
  { name: "Book", icon: Book },
  { name: "Calendar", icon: Calendar },
  { name: "Mail", icon: Mail },
  { name: "Phone", icon: Phone },
  { name: "Settings", icon: Settings },
  { name: "Home", icon: Home },
  { name: "Car", icon: Car },
  { name: "Plane", icon: Plane },
  { name: "MapPin", icon: MapPin },
  { name: "Clock", icon: Clock },
  { name: "CheckCircle", icon: CheckCircle },
  { name: "AlertCircle", icon: AlertCircle },
  { name: "Info", icon: Info },
  { name: "Plus", icon: Plus },
  { name: "Search", icon: Search },
  { name: "Edit", icon: Edit },
  { name: "Share", icon: Share },
  { name: "Eye", icon: Eye },
  { name: "Lock", icon: Lock },
  { name: "Unlock", icon: Unlock },
  { name: "ChevronRight", icon: ChevronRight },
  { name: "ChevronLeft", icon: ChevronLeft },
  { name: "ChevronUp", icon: ChevronUp },
  { name: "ChevronDown", icon: ChevronDown },
  { name: "Trash2", icon: Trash2 },
  { name: "Copy", icon: Copy },
  { name: "Download", icon: Download },
  { name: "Upload", icon: Upload },
  { name: "Minus", icon: Minus },
  { name: "X", icon: X },
  { name: "Check", icon: Check },
  { name: "Filter", icon: Filter },
  { name: "EyeOff", icon: EyeOff },
];

interface IconPickerProps {
  value: string;
  onChange: (iconName: string) => void;
}

export function IconPicker({ value, onChange }: IconPickerProps) {
  const [open, setOpen] = useState(false);
  
  const selectedIcon = availableIcons.find(icon => icon.name === value);
  const SelectedIconComponent = selectedIcon?.icon || Building2;

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          className="w-full justify-start gap-2 bg-gray-900 border-gray-700 text-white hover:bg-gray-800 hover:border-white"
        >
          <SelectedIconComponent className="h-4 w-4" />
          <span>{selectedIcon?.name || "Select Icon"}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80 p-0 bg-gray-900 border-gray-700" align="start">
        <ScrollArea className="h-64">
          <div className="grid grid-cols-6 gap-2 p-4">
            {availableIcons.map((iconItem) => {
              const IconComponent = iconItem.icon;
              return (
                <Button
                  key={iconItem.name}
                  variant="ghost"
                  size="sm"
                  className={`h-10 w-10 p-0 hover:bg-gray-800 ${
                    value === iconItem.name ? "bg-white text-black" : "text-white"
                  }`}
                  onClick={() => {
                    onChange(iconItem.name);
                    setOpen(false);
                  }}
                >
                  <IconComponent className="h-4 w-4" />
                </Button>
              );
            })}
          </div>
        </ScrollArea>
      </PopoverContent>
    </Popover>
  );
}