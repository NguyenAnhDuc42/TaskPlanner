export type Theme = "Light" | "Dark" | "System" | "Mars" | "DeepSpace" | "Boreal";

export const THEME_LABELS: Record<Theme, string> = {
  Light: "Light",
  Dark: "Dark",
  System: "System",
  Mars: "Mars",
  DeepSpace: "Deep Space",
  Boreal: "Boreal",
};

export function getThemeLabel(theme: Theme) {
  return THEME_LABELS[theme] ?? theme;
}
