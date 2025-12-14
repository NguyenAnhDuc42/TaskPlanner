import type { z } from "zod"


export interface ZodValidationErrors {
  [key: string]: string
}


export function getZodErrors(error: z.ZodError): ZodValidationErrors {
  const errors: ZodValidationErrors = {}

  error.errors.forEach((err) => {
    const path = err.path.join(".")
    errors[path] = err.message
  })

  return errors
}

export function getFieldError(
  fieldName: string,
  validationErrors: Record<string, string[]>,
  zodErrors?: ZodValidationErrors,
): string | undefined {
  // Check Zod errors first (client-side validation)
  if (zodErrors?.[fieldName]) {
    return zodErrors[fieldName]
  }

  // Then check server validation errors
  const fieldErrors = validationErrors[fieldName] || validationErrors[fieldName.toLowerCase()]
  return fieldErrors?.[0]
}
