import { PriorityBreakdownChart } from "./(component)/dashboard/priority-breakdown-chart";
import { PriorityTaskList } from "./(component)/dashboard/priority-task-list";

export default function Page() {
  return (
    <main>
      <PriorityTaskList />
      <PriorityBreakdownChart />
    </main>
  );
}
