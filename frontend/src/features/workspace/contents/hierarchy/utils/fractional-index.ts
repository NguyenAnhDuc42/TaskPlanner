const Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

/**
 * Fractional indexing implementation matching the backend's LexoRank strategy.
 * Provides stable, high-precision sequencing for reordering.
 */

/** Starting midpoint key */
export function fractionalStart(): string {
  return "V";
}

/** Key sorting after the given key */
export function fractionalAfter(key?: string | null | undefined): string {
  return fractionalBetween(key, null);
}

/** Key sorting before the given key */
export function fractionalBefore(key?: string | null | undefined): string {
  return fractionalBetween(null, key);
}

/**
 * Generates a string that sorts between `before` and `after`.
 */
export function fractionalBetween(before?: string | null | undefined, after?: string | null | undefined): string {
  const b = before ?? "   "; // Default low (using space as base)
  const a = after ?? "zzz";  // Default high

  // Safety: Inverted or equal keys
  if (b >= a) {
    return fractionalAfter(b);
  }

  let result = "";
  const n = Math.max(b.length, a.length) + 1;

  for (let i = 0; i < n; i++) {
    const bc = i < b.length ? b[i] : String.fromCharCode(0);
    const ac = i < a.length ? a[i] : String.fromCharCode(127);

    if (bc === ac) {
      result += bc;
      continue;
    }

    const bIndex = Alphabet.indexOf(bc);
    const aIndex = Alphabet.indexOf(ac);

    // Handle characters not in alphabet (padding or boundary)
    const bIdx = bIndex === -1 ? -1 : bIndex;
    const aIdx = aIndex === -1 ? Alphabet.length : aIndex;

    const midIndex = Math.floor((bIdx + aIdx) / 2);

    // If we found a midpoint between characters
    if (midIndex > bIdx) {
      result += Alphabet[midIndex];
      break;
    }

    // No room at this character position (e.g., '1' and '2') — go one level deeper
    result += bc;

    // If b has no more characters, start from midpoint of alphabet for next level
    if (i >= b.length - 1) {
      result += Alphabet[Math.floor(Alphabet.length / 2)];
      break;
    }
  }

  return result;
}
