export type WorkspaceVariant = "Personal" | "Team" | "Company";

export const WORKSPACE_VARIANT_LABELS: Record<WorkspaceVariant, string> = {
  Personal: "Personal",
  Team: "Team",
  Company: "Company",
};

// render helper
export function getWorkspaceVariantLabel(v: WorkspaceVariant) {
  return WORKSPACE_VARIANT_LABELS[v] ?? v; // forward-compatible fallback
}