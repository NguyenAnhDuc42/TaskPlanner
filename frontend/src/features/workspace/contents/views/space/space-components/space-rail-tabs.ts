import { FileText, History, ListTodo } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export type SpaceRailTabKey = "detail" | "items" | "activity";

export interface SpaceRailTabDef {
  key: SpaceRailTabKey;
  label: string;
  icon: LucideIcon;
}

export const SPACE_RAIL_TABS: Record<SpaceRailTabKey, SpaceRailTabDef> = {
  detail: { key: "detail", label: "Document", icon: FileText },
  items: { key: "items", label: "Tasks", icon: ListTodo },
  activity: { key: "activity", label: "Activity", icon: History },
};

export const DEFAULT_SPACE_RAIL_TAB_ORDER: SpaceRailTabKey[] = ["detail", "items", "activity"];

export function isSpaceRailTabKey(value: unknown): value is SpaceRailTabKey {
  return value === "detail" || value === "items" || value === "activity";
}

export function normalizeTabOrder(order: unknown): SpaceRailTabKey[] {
  const valid = Array.isArray(order) ? order.filter(isSpaceRailTabKey) : [];
  const deduped = Array.from(new Set(valid));
  const missing = DEFAULT_SPACE_RAIL_TAB_ORDER.filter((key) => !deduped.includes(key));
  return [...deduped, ...missing];
}
