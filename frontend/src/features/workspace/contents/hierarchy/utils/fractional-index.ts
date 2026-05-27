/**
 * Thin wrapper around the battle-tested `fractional-indexing` library by rocicorp.
 * https://github.com/rocicorp/fractional-indexing
 *
 * Includes a `safeKey` sanitizer that converts legacy keys (from the old
 * custom implementation) to `null` so the library never throws on stale data.
 */
import { generateKeyBetween, generateNKeysBetween } from "fractional-indexing";

/**
 * Rocicorp keys must start with a lowercase letter (positive) or uppercase
 * letter (negative) as an integer-length prefix, followed by digits.
 * Any key that doesn't match is treated as an unbound end (null).
 */
export function safeKey(key: string | null | undefined): string | null {
  if (key == null || key === "") return null;
  // Rocicorp format: letter prefix indicating digit-count, followed by digits
  // e.g. "a0", "a9", "b10", "Z9" — letter MUST be followed by at least one digit
  if (/^[a-zA-Z][0-9]/.test(key)) return key;
  return null; // Legacy key ("0", "V", "1", etc.) — treat as unbound
}

/**
 * Returns a key that sorts between `lo` and `hi`.
 * Pass `null`/`undefined` for either bound to mean "no bound" (−∞ or +∞).
 */
export function fractionalBetween(
  lo?: string | null,
  hi?: string | null
): string {
  return generateKeyBetween(safeKey(lo), safeKey(hi));
}

/** Returns a key that sorts after `key` (key → +∞). */
export function fractionalAfter(key?: string | null): string {
  return generateKeyBetween(safeKey(key), null);
}

/** Returns a key that sorts before `key` (−∞ → key). */
export function fractionalBefore(key?: string | null): string {
  return generateKeyBetween(null, safeKey(key));
}

/** Returns the canonical starting key (middle of the alphabet). */
export function fractionalStart(): string {
  return generateKeyBetween(null, null); // "a0"
}

/**
 * Generates `count` evenly-spaced keys between `lo` and `hi`.
 * Prefer this over calling `fractionalBetween` in a loop for batch inserts —
 * the keys stay much shorter.
 */
export function fractionalBetweenN(
  lo: string | null,
  hi: string | null,
  count: number
): string[] {
  return generateNKeysBetween(safeKey(lo), safeKey(hi), count);
}