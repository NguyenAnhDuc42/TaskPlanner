const Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
const AlphabetLength = Alphabet.length;

// O(1) lookup table built once
const AlphabetIndex = new Int8Array(128).fill(-1);
for (let i = 0; i < AlphabetLength; i++) {
  AlphabetIndex[Alphabet.charCodeAt(i)] = i;
}

function getIndex(c: string): number {
  const code = c.charCodeAt(0);
  return code < 128 ? AlphabetIndex[code] : -1;
}

export function fractionalStart(): string  { return "V"; }
export function fractionalAfter(key?: string | null):  string { return fractionalBetween(key, null); }
export function fractionalBefore(key?: string | null): string { return fractionalBetween(null, key); }

export function fractionalBetween(before?: string | null, after?: string | null): string {
  const b = before ?? "";
  const a = after  ?? "";

  if (!b && !a) return "V";

  const n = Math.max(b.length, a.length) + 1;
  let result = "";

  for (let i = 0; i < n; i++) {
    const bc = i < b.length ? b[i] : "\0";
    const ac = i < a.length ? a[i] : "\x7f";

    if (bc === ac) { result += bc; continue; }

    let bIdx = getIndex(bc);
    let aIdx = getIndex(ac);
    if (aIdx === -1) aIdx = AlphabetLength;

   const midIdx = Math.floor((Math.max(bIdx, 0) + aIdx) / 2);

    if (midIdx > bIdx) {
      result += Alphabet[midIdx];
      break;
    }

    result += bc;
    if (i >= b.length - 1) {
      result += Alphabet[Math.floor(AlphabetLength / 2)];
      break;
    }
  }

  return result;
}