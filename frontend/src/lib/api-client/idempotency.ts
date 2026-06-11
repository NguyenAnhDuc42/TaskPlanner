export async function generateRequestHash(method: string, url: string, data: unknown, params: unknown): Promise<string> {
  const signature = {
    method: method.toLowerCase(),
    url,
    data: data || null,
    params: params || null
  };
  const rawString = JSON.stringify(signature);
  const msgUint8 = new TextEncoder().encode(rawString);
  const hashBuffer = await crypto.subtle.digest("SHA-256", msgUint8);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
}
