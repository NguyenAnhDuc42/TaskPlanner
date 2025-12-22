import { toast } from 'sonner'
import type { ProblemDetails } from '@/types/problem-details'

export async function runAction<T>(
  action: () => Promise<T>,
  options?: {
    successMessage?: string
    fallbackError?: string
  },
) {
  try {
    const result = await action()
    if (options?.successMessage) toast.success(options.successMessage)

    return result
  } catch (err: any) {
    if (err instanceof Response) {
      const pd = await err.json()
      // Validation errors
      if (pd.errors && Object.keys(pd.errors).length > 0) {
        toast.error(pd.title ?? 'Validation failed')
        throw pd
      }
      toast.error(pd.title ?? 'Request failed', {
        description: pd.detail,
      })
      throw pd
    }
    toast.error(options?.fallbackError ?? 'Something went wrong')
    throw err
  }
}

