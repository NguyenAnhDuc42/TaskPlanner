import { type LucideIcon } from "lucide-react";

export type ContentPage =
  | "projects"
  | "spaces"
  | "folders"
  | "tasks"
  | "calendar"
  | "members"
  | "settings"
  | "command-center"
  | "inbox";

export interface NavItem {
  id: string;
  icon: LucideIcon;
  label: string;
}
