import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

const AVATAR_COLORS = [
  "bg-slate-800",
  "bg-blue-800",
  "bg-emerald-700",
  "bg-amber-600",
  "bg-red-800",
  "bg-orange-700",
  "bg-cyan-800",
  "bg-indigo-800",
  "bg-stone-800",
  "bg-teal-800",
  "bg-sky-700",
  "bg-zinc-800",
];

function getStringHash(str: string) {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  return Math.abs(hash);
}

export interface UserAvatarProps {
  name: string;
  avatarUrl?: string | null;
  className?: string;
  fallbackClassName?: string;
}

export function UserAvatar({
  name,
  avatarUrl,
  className,
  fallbackClassName,
}: UserAvatarProps) {
  const initials = name
    .split(" ")
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase() || "?";

  // Use the name string to pick a deterministic color
  const colorClass = AVATAR_COLORS[getStringHash(name) % AVATAR_COLORS.length];

  return (
    <Avatar className={cn("shrink-0", className)}>
      {avatarUrl && <AvatarImage src={avatarUrl} alt={name} />}
      <AvatarFallback 
        className={cn("text-white font-black leading-none", colorClass, fallbackClassName)}
      >
        {initials}
      </AvatarFallback>
    </Avatar>
  );
}
