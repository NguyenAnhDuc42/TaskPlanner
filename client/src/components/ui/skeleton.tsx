"use client"

import { cn } from "@/lib/utils"
import React from "react"

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  width?: number | string
}

const Skeleton = React.forwardRef<HTMLDivElement, SkeletonProps>(
  ({ className, width, style, ...props }, ref) => {
    const [randomWidth, setRandomWidth] = React.useState(() =>
      width ? "auto" : `${Math.floor(Math.random() * (80 - 40 + 1)) + 40}%`,
    )

    React.useEffect(() => {
      if (width === undefined) {
        setRandomWidth(
          `${Math.floor(Math.random() * (80 - 40 + 1)) + 40}%`,
        )
      }
    }, [width])

    const skeletonStyle = width
      ? { ...style, width }
      : { ...style, width: randomWidth }

    return (
      <div
        ref={ref}
        data-slot="skeleton"
        className={cn("bg-accent animate-pulse rounded-md", className)}
        style={skeletonStyle}
        {...props}
      />
    )
  },
)
Skeleton.displayName = "Skeleton"

export { Skeleton }
