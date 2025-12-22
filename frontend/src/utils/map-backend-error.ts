export function mapBackendErrors(
  errors: { propertyName: string; errorMessage: string }[]
) {
  const map: Record<string, string[]> = {}
  for (const e of errors) {
    map[e.propertyName] ??= []
    map[e.propertyName].push(e.errorMessage)
  }
  return map
}