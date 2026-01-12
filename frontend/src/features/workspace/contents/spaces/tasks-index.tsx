"use client";

export default function TasksIndex() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Tasks</h1>
        <p className="text-muted-foreground">
          Manage and track your project tasks.
        </p>
      </div>

      <div className="rounded-xl border bg-card p-8 text-center border-dashed">
        <p className="text-muted-foreground">
          No tasks found. Create your first task to get started.
        </p>
      </div>
    </div>
  );
}
