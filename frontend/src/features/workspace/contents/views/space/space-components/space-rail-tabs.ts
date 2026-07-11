import { FileText, ListTodo, MessageSquare } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export type SpaceRailTabKey = "detail" | "items" | "comms";

export interface SpaceRailTabDef {
  key: SpaceRailTabKey;
  label: string;
  icon: LucideIcon;
}

export const SPACE_RAIL_TABS: Record<SpaceRailTabKey, SpaceRailTabDef> = {
  // "detail" renders SpaceDocumentsPanel (the space's single doc, Notion-style hero+editor) —
  // "Document" describes what it actually shows, not "Board"/"Overview" (the kanban lives under
  // "items", labeled "Tasks").
  detail: { key: "detail", label: "Document", icon: FileText },
  items: { key: "items", label: "Tasks", icon: ListTodo },
  comms: { key: "comms", label: "Communications", icon: MessageSquare },
};

export const DEFAULT_SPACE_RAIL_TAB_ORDER: SpaceRailTabKey[] = ["detail", "items", "comms"];

export function isSpaceRailTabKey(value: unknown): value is SpaceRailTabKey {
  return value === "detail" || value === "items" || value === "comms";
}

// Guards against a stale/corrupt localStorage order (e.g. a tab key that no longer exists) —
// always returns a complete, valid order by falling back to the default for anything missing.
export function normalizeTabOrder(order: unknown): SpaceRailTabKey[] {
  const valid = Array.isArray(order) ? order.filter(isSpaceRailTabKey) : [];
  const deduped = Array.from(new Set(valid));
  const missing = DEFAULT_SPACE_RAIL_TAB_ORDER.filter((key) => !deduped.includes(key));
  return [...deduped, ...missing];
}
