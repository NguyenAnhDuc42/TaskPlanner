import { type LucideIcon } from "lucide-react";

export type ContentPage =
  | "projects"
  | "spaces"
  | "folders"
  | "tasks"
  | "calendar"
  | "members"
  | "settings"
  | "inbox";

export interface NavItem {
  id: string;
  icon: LucideIcon;
  label: string;
}
