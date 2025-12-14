"use client"

import { FormDescription } from "@/components/ui/form"

import { useEffect } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import { format } from "date-fns"
import { CalendarIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Checkbox } from "@/components/ui/checkbox"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Calendar } from "@/components/ui/calendar"
import { cn } from "@/lib/utils" // Assuming this utility exists

import { useCreateTaskInList } from "@/features/list/list-hooks" // Changed to useCreateTaskInList
import type { PlanTaskStatus } from "@/types/task" // Your PlanTaskStatus type
import type { CreateTaskInListRequest } from "@/features/list/list-type" // Changed to CreateTaskInListRequest

// Define the form schema using Zod
const formSchema = z.object({
  name: z.string().min(1, "Task name is required"),
  description: z.string().optional(),
  priority: z.coerce.number().min(0).max(5).default(0),
  status: z.enum(["ToDo", "InProgress", "InReview", "Done"]),
  startDate: z.date().optional().nullable(),
  dueDate: z.date().optional().nullable(),
  isPrivate: z.boolean().default(false),
})

interface CreateTaskFormProps {
  isOpen: boolean
  onClose: () => void
  initialStatus: PlanTaskStatus
  listId: string
  // Removed workspaceId and spaceId as they are not part of CreateTaskInListRequest
}

export function CreateTaskForm({ isOpen, onClose, initialStatus, listId }: CreateTaskFormProps) {
  const createTaskInListMutation = useCreateTaskInList()

  const form = useForm({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: "",
      description: "",
      priority: 0,
      status: initialStatus,
      startDate: null,
      dueDate: null,
      isPrivate: false,
    },
  })

  // Update default status when initialStatus prop changes
  useEffect(() => {
    form.reset({
      ...form.getValues(), // Keep other values if form is re-opened
      status: initialStatus,
    })
  }, [initialStatus, form])

  const onSubmit = async (values: z.infer<typeof formSchema>) => {
    const taskData: CreateTaskInListRequest = {
      ...values,
      startDate: values.startDate ? values.startDate.toISOString() : null,
      dueDate: values.dueDate ? values.dueDate.toISOString() : null,
      listId,
    }

    try {
      await createTaskInListMutation.mutateAsync(taskData)
      form.reset()
      onClose()
    } catch (error) {
      // Error handling is done in useCreateTaskInList hook via toast
      console.error("Failed to create task:", error)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[425px] bg-[#242424] text-gray-100 border-gray-700">
        <DialogHeader>
          <DialogTitle className="text-gray-100">Create New Task</DialogTitle>
          <DialogDescription className="text-gray-400">Fill in the details for your new task.</DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="grid gap-4 py-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-gray-300">Task Name</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="e.g., Finish project report"
                      {...field}
                      className="bg-[#1a1a1a] border-gray-700 text-gray-100 focus:ring-blue-500"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-gray-300">Description</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="Add a detailed description..."
                      {...field}
                      className="bg-[#1a1a1a] border-gray-700 text-gray-100 focus:ring-blue-500"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="status"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-gray-300">Status</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger className="bg-[#1a1a1a] border-gray-700 text-gray-100 focus:ring-blue-500">
                        <SelectValue placeholder="Select a status" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent className="bg-gray-800 text-gray-100 border-gray-700">
                      <SelectItem value="ToDo">To Do</SelectItem>
                      <SelectItem value="InProgress">In Progress</SelectItem>
                      <SelectItem value="InReview">In Review</SelectItem>
                      <SelectItem value="Done">Done</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="priority"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-gray-300">Priority (0-5)</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min="0"
                      max="5"
                      placeholder="0"
                      {...field}
                      onChange={(e) => field.onChange(Number.parseInt(e.target.value))}
                      className="bg-[#1a1a1a] border-gray-700 text-gray-100 focus:ring-blue-500"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="dueDate"
              render={({ field }) => (
                <FormItem className="flex flex-col">
                  <FormLabel className="text-gray-300">Due Date</FormLabel>
                  <Popover>
                    <PopoverTrigger asChild>
                      <FormControl>
                        <Button
                          variant={"outline"}
                          className={cn(
                            "w-full pl-3 text-left font-normal bg-[#1a1a1a] border-gray-700 text-gray-100 hover:bg-gray-700",
                            !field.value && "text-gray-400",
                          )}
                        >
                          {field.value ? format(field.value, "PPP") : <span>Pick a date</span>}
                          <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                        </Button>
                      </FormControl>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0 bg-gray-800 border-gray-700" align="start">
                      <Calendar
                        mode="single"
                        selected={field.value || undefined}
                        onSelect={field.onChange}
                        initialFocus
                        className="text-gray-100"
                        classNames={{
                          day_selected: "bg-blue-600 text-white hover:bg-blue-700",
                          day_today: "bg-gray-700 text-gray-100",
                          day_outside: "text-gray-500 opacity-50",
                          day_range_middle: "bg-gray-700 text-gray-100",
                          day_hidden: "invisible",
                          caption_label: "text-gray-100",
                          nav_button: "text-gray-100 hover:bg-gray-700",
                          head_row: "text-gray-400",
                          weeknumber: "text-gray-400",
                          cell: "text-gray-100",
                        }}
                      />
                    </PopoverContent>
                  </Popover>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="startDate"
              render={({ field }) => (
                <FormItem className="flex flex-col">
                  <FormLabel className="text-gray-300">Start Date</FormLabel>
                  <Popover>
                    <PopoverTrigger asChild>
                      <FormControl>
                        <Button
                          variant={"outline"}
                          className={cn(
                            "w-full pl-3 text-left font-normal bg-[#1a1a1a] border-gray-700 text-gray-100 hover:bg-gray-700",
                            !field.value && "text-gray-400",
                          )}
                        >
                          {field.value ? format(field.value, "PPP") : <span>Pick a date</span>}
                          <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                        </Button>
                      </FormControl>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0 bg-gray-800 border-gray-700" align="start">
                      <Calendar
                        mode="single"
                        selected={field.value || undefined}
                        onSelect={field.onChange}
                        initialFocus
                        className="text-gray-100"
                        classNames={{
                          day_selected: "bg-blue-600 text-white hover:bg-blue-700",
                          day_today: "bg-gray-700 text-gray-100",
                          day_outside: "text-gray-500 opacity-50",
                          day_range_middle: "bg-gray-700 text-gray-100",
                          day_hidden: "invisible",
                          caption_label: "text-gray-100",
                          nav_button: "text-gray-100 hover:bg-gray-700",
                          head_row: "text-gray-400",
                          weeknumber: "text-gray-400",
                          cell: "text-gray-100",
                        }}
                      />
                    </PopoverContent>
                  </Popover>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="isPrivate"
              render={({ field }) => (
                <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border border-gray-700 p-4">
                  <FormControl>
                    <Checkbox checked={field.value} onCheckedChange={field.onChange} />
                  </FormControl>
                  <div className="space-y-1 leading-none">
                    <FormLabel className="text-gray-300">Private Task</FormLabel>
                    <FormDescription className="text-gray-400">
                      Only visible to you and assigned members.
                    </FormDescription>
                  </div>
                </FormItem>
              )}
            />
            <Button
              type="submit"
              className="w-full bg-blue-600 hover:bg-blue-700 text-white"
              disabled={createTaskInListMutation.isPending}
            >
              {createTaskInListMutation.isPending ? "Creating..." : "Create Task"}
            </Button>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
