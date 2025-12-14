"use client"

import { DueSoonTaskList } from "./(component)/dashboard/due-soon-task-list"
import { LongCardPlaceholder } from "./(component)/dashboard/long-card-placeholder"
import { PriorityBreakdownChart } from "./(component)/dashboard/priority-breakdown-chart"
import { PriorityTaskList } from "./(component)/dashboard/priority-task-list"


export default function Page() {
  return (
    <div className="grid grid-cols-4 gap-6 w-full h-full">
      {/* First row */}
      <div className="col-span-2 h-full">
        <PriorityTaskList/>
      </div>

      <div className="col-span-1 h-full">
        <LongCardPlaceholder className="h-full" />
      </div>

      <div className="col-span-1 flex flex-col gap-6 h-full">
        <PriorityBreakdownChart className="flex-1" />
        <PriorityBreakdownChart className="flex-1" />
      </div>

      {/* Second row */}
      <div className="col-span-4">
        <DueSoonTaskList className="h-full" />
      </div>
    </div>
  )
}
