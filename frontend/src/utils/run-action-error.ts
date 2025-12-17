import { toast } from 'sonner'

export async function runAction<T>(
  action: () => Promise<T>,
  options?: {
    successMessage?: string
    fallbackError?: string
  },
) {
  try {
    const result = await action()
    if (options?.successMessage) {
      toast.success(options.successMessage)
    }
    return result
  }  catch (err) {
    if (err instanceof Response) {
      // try parse JSON first (structured error), fallback to text
      try {
        const json = await err.json();
        const message = json?.message ?? JSON.stringify(json);
        toast.error(String(message));
        return;
      } catch {
        const text = (await err.text().catch(() => null)) ?? options?.fallbackError ?? "Something went wrong";
        toast.error(String(text));
        return;
      }
    }
    toast.error(options?.fallbackError ?? "Something went wrong");
    return;
  }
}
