import * as Icons from "lucide-react";
import { cn } from "@/lib/utils";

interface Props {
  name: string;
  color?: string;
  size?: number;
  className?: string;
}

// Lucide icon names are strictly PascalCase alphanumeric — anything else is an emoji or text
const isLucideName = (name: string) => /^[A-Za-z0-9]+$/.test(name);

export const DynamicIcon = ({ name, color = "", size = 24, className }: Props) => {
  if (!name) {
    return (
      <Icons.HelpCircle
        color={color || undefined}
        size={size}
        className={cn("theme-adaptive-icon", className)}
        style={color ? { "--icon-color": color } as React.CSSProperties : undefined}
      />
    );
  }

  if (!isLucideName(name)) {
    // Emoji or arbitrary text — render as inline span
    return (
      <span
        style={{ fontSize: size * 0.85, lineHeight: 1 }}
        className={className}
        aria-hidden="true"
      >
        {name}
      </span>
    );
  }

  const LucideIcon = Icons[name as keyof typeof Icons] as React.ComponentType<{ color?: string; size?: number; className?: string; style?: React.CSSProperties }> | undefined;

  if (!LucideIcon) {
    return (
      <Icons.HelpCircle
        color={color || undefined}
        size={size}
        className={cn("theme-adaptive-icon", className)}
        style={color ? { "--icon-color": color } as React.CSSProperties : undefined}
      />
    );
  }

  return (
    <LucideIcon
      color={color || undefined}
      size={size}
      className={cn("theme-adaptive-icon", className)}
      style={color ? { "--icon-color": color } as React.CSSProperties : undefined}
    />
  );
};
