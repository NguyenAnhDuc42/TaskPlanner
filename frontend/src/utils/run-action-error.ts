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
  } catch (err) {
    if (err instanceof Response) {
      const message = await err.text()
      toast.error(message)
      return
    }

    toast.error(options?.fallbackError ?? 'Something went wrong')
  }
}
