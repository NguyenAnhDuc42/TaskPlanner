"use client"

import { Skeleton } from "@/components/ui/skeleton"

function SkeletonRow() {
  return (
    <div className="grid grid-cols-7 gap-4 py-3 px-4">
      <div className="flex items-center">
        <Skeleton className="h-4 w-4 rounded" />
      </div>
      <div className="flex items-center gap-3">
        <Skeleton className="h-8 w-8 rounded-full" />
        <Skeleton className="h-4 w-24" />
      </div>
      <div className="flex items-center">
        <Skeleton className="h-4 w-40" />
      </div>
      <div className="flex items-center">
        <Skeleton className="h-6 w-16 rounded-full" />
      </div>
      <div className="flex items-center">
        <Skeleton className="h-4 w-20" />
      </div>
      <div className="flex items-center">
        <Skeleton className="h-4 w-20" />
      </div>
    </div>
  )
}

export function MembersTableSkeleton() {
  return (
    <div className="space-y-1">
      {Array.from({ length: 5 }).map((_, i) => (
        <SkeletonRow key={i} />
      ))}
    </div>
  )
}