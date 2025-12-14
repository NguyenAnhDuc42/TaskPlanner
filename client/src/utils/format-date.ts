export const formatDate = (dueDate: string | null) => {
  if (!dueDate) return ""
  return new Date(dueDate).toLocaleDateString("en-US", {
    month: "numeric",
    day: "numeric",
    year: "numeric",
  })
}