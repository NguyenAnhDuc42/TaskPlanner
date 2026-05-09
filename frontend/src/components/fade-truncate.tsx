import React from "react"
import { cn } from "@/lib/utils"

type FadeTruncateProps = {
  text: string
  lines?: 1 | 2 | 3 | 4
  className?: string
  as?: "span" | "div" | "p"
  showTooltip?: boolean
  onTruncate?: (isTruncated: boolean) => void
}

export const FadeTruncate = React.memo(function FadeTruncate({
  text,
  lines = 1,
  className,
  as = "span",
  showTooltip = true,
  onTruncate,
}: FadeTruncateProps) {
  const Component = as
  const ref = React.useRef<HTMLElement>(null)

  React.useEffect(() => {
    if (!ref.current || !onTruncate) return

    const element = ref.current
    const isTruncated =
      lines === 1
        ? element.scrollWidth > element.clientWidth
        : element.scrollHeight > element.clientHeight

    onTruncate(isTruncated)
  }, [text, lines, onTruncate])

  const truncateClass = lines === 1 ? "truncate" : `line-clamp-${lines}`

  return (
    <Component
      ref={ref as any}
      className={cn("block min-w-0 w-full", truncateClass, className)}
      title={showTooltip ? text : undefined}
    >
      {text}
    </Component>
  )
})

FadeTruncate.displayName = "FadeTruncate"