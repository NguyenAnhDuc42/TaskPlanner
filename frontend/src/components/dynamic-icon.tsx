import * as Icons from "lucide-react";

interface Props {
  name: string;
  color?: string;
  size?: number;
  className?: string;
}

export const DynamicIcon = ({ name, color = "", size = 24, className }: Props) => {
  const LucideIcon = Icons[name as keyof typeof Icons] as any;

  if (!LucideIcon) {
    return <Icons.HelpCircle color={color} size={size} className={className} />;
  }

  return <LucideIcon color={color} size={size} className={className} />;
};
