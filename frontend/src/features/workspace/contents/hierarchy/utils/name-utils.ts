const NAME_CHAR_LIMIT = 20;

export function clampName(name: string | undefined | null, limit = NAME_CHAR_LIMIT) {
  if (!name) return "";
  if (name.length <= limit) return name;
  return `${name.slice(0, Math.max(0, limit - 1))}…`;
}
