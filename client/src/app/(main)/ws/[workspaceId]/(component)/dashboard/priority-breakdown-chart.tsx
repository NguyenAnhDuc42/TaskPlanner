"use client"

import { useMemo, useState } from "react"
import { Pie, PieChart, Cell } from "recharts"
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart"
import { useTasksMetadata } from "@/features/task/task-hooks"
import { useWorkspaceId } from "@/utils/current-layer-id"
import { Loader2 } from "lucide-react"
import { Priority } from "@/utils/priority-utils"
import { cn } from "@/lib/utils" // Import cn

const chartConfig = {
  [Priority.Urgent]: { label: "Urgent", color: "#ef4444" },
  [Priority.High]: { label: "High", color: "#f59e0b" },
  [Priority.Medium]: { label: "Medium", color: "#3b82f6" },
  [Priority.Low]: { label: "Low", color: "#10b981" },
  [Priority.Clear]: { label: "Clear", color: "#6b7280" },
}

export function PriorityBreakdownChart({ className }: { className?: string }) { // Accept className prop
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null)
  const workspaceId = useWorkspaceId()

  const { data: tasksMetadata, isLoading, isError } = useTasksMetadata({
    workspaceId,
  })

  const chartData = useMemo(() => {
    if (!tasksMetadata?.priorityBreakdown) return []

    return Object.entries(tasksMetadata.priorityBreakdown)
      .map(([priority, count]) => {
        const config = chartConfig[priority as Priority]
        return config ? { priority, count, color: config.color } : null
      })
      .filter(Boolean) as { priority: string; count: number; color: string }[]
  }, [tasksMetadata])

  if (isLoading) {
    return (
      <Card className={cn("w-80 h-full flex items-center justify-center", className)}> {/* Apply className */}
        <Loader2 className="h-8 w-8 animate-spin" />
      </Card>
    )
  }

  if (isError) {
    return (
      <Card className={cn("w-80 h-full flex items-center justify-center", className)}> {/* Apply className */}
        <p className="text-destructive">Error loading chart data.</p>
      </Card>
    )
  }

  return (
    <Card className={cn("w-80", className)}> {/* Apply className */}
      <CardHeader>
        <CardTitle>Workload by Priority</CardTitle>
        <CardDescription>Overdue tasks: {tasksMetadata?.overdueCount ?? 0}</CardDescription>
      </CardHeader>
      <CardContent>
        <ChartContainer config={chartConfig} className="h-64 w-full">
          <PieChart>
            <Pie
              data={chartData}
              cx="50%"
              cy="50%"
              innerRadius={40}
              outerRadius={80}
              paddingAngle={2}
              dataKey="count"
              onMouseEnter={(_, index) => setHoveredIndex(index)}
              onMouseLeave={() => setHoveredIndex(null)}
            >
              {chartData.map((entry, index) => (
                <Cell
                  key={`cell-${index}`}
                  fill={hoveredIndex !== null && hoveredIndex !== index ? `${entry.color}60` : entry.color}
                  style={{
                    filter: hoveredIndex === index ? "drop-shadow(0 0 8px rgba(255,255,255,0.3))" : "none",
                    transform: hoveredIndex === index ? "scale(1.05)" : "scale(1)",
                    transformOrigin: "center",
                    transition: "all 0.2s ease-in-out",
                  }}
                />
              ))}
            </Pie>
            <ChartTooltip
              content={({ active, payload }) => {
                if (active && payload && payload.length) {
                  const data = payload[0].payload
                  return (
                    <ChartTooltipContent
                      active={active}
                      payload={[
                        {
                          ...payload[0],
                          name: data.priority.toUpperCase(),
                          value: data.count,
                        },
                      ]}
                      hideLabel
                    />
                  )
                }
                return null
              }}
            />
          </PieChart>
        </ChartContainer>
      </CardContent>
    </Card>
  )
}
