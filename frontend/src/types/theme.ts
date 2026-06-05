export type Theme = "Light" | "Dark";

export const THEME_LABELS: Record<Theme, string> = {
  Light: "Light",
  Dark: "Dark",
};

export function getThemeLabel(theme: Theme) {
  return THEME_LABELS[theme] ?? theme;
}
