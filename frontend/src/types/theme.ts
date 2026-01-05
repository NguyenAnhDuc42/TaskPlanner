export type Theme = "Light" | "Dark" | "System";

export const THEME_LABELS: Record<Theme, string> = {
  Light: "Light",
  Dark: "Dark",
  System: "System",
};

export function getThemeLabel(theme: Theme) {
  return THEME_LABELS[theme] ?? theme;
}
