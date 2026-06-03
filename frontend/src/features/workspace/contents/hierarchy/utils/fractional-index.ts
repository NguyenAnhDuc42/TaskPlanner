
import { generateKeyBetween, generateNKeysBetween } from "fractional-indexing";

export function safeKey(key: string | null | undefined): string | null {
  if (key == null || key === "") return null;

  try {
    generateKeyBetween(key, null);
    generateKeyBetween(null, key);
    return key;
  } catch {
    return null;
  }
}

export function fractionalBetween(
  lo?: string | null,
  hi?: string | null
): string {
  const safeLo = safeKey(lo);
  const safeHi = safeKey(hi);

  if (safeLo !== null && safeHi !== null) {
    if (safeLo >= safeHi) {
      return generateKeyBetween(safeLo, null);
    }
  }

  return generateKeyBetween(safeLo, safeHi);
}


export function fractionalAfter(key?: string | null): string {
  return generateKeyBetween(safeKey(key), null);
}


export function fractionalBefore(key?: string | null): string {
  return generateKeyBetween(null, safeKey(key));
}

export function fractionalStart(): string {
  return generateKeyBetween(null, null); // "a0"
}

export function fractionalBetweenN(
  lo: string | null,
  hi: string | null,
  count: number
): string[] {
  return generateNKeysBetween(safeKey(lo), safeKey(hi), count);
}