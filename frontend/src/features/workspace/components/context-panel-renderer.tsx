import { observer } from "mobx-react-lite";
import { TaskDetailCanvas } from "@/features/workspace/contents/views/task/components/task-detail-canvas";

interface ContextPanelRendererProps {
  data: {
    type: "task" | "space" | "project";
    id: string;
  };
}

export const ContextPanelRenderer = observer(function ContextPanelRenderer({ data }: ContextPanelRendererProps) {
  // TODO: Wire up real detail components as they're built
  if (data.type === "task" && data.id) {
    return <TaskDetailCanvas taskId={data.id} />;
  }
  // if (data.type === "space") return <SpaceDetail space={data} />;

  return (
    <pre className="text-xs text-muted-foreground p-2">
      {JSON.stringify(data, null, 2)}
    </pre>
  );
});
