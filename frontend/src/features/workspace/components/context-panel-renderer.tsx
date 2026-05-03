interface ContextPanelRendererProps {
  data: { type: string; [key: string]: any };
}

export function ContextPanelRenderer({ data }: ContextPanelRendererProps) {
  // TODO: Wire up real detail components as they're built
  // if (data.type === "task") return <TaskDetail task={data} />;
  // if (data.type === "space") return <SpaceDetail space={data} />;
  // if (data.type === "folder") return <FolderDetail folder={data} />;

  return (
    <pre className="text-xs text-muted-foreground p-2">
      {JSON.stringify(data, null, 2)}
    </pre>
  );
}
