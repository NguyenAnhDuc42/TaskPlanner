import { useState } from "react";
import { useCreateWidget } from "../dashboard-api";
import { WidgetType } from "@/types/widget-type";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export function CreateWidgetForm({ 
  dashboardId, 
  onSuccess 
}: { 
  dashboardId: string; 
  onSuccess: () => void 
}) {
  const [type, setType] = useState<WidgetType>(WidgetType.TaskList);
  const createWidget = useCreateWidget();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!dashboardId) return;

    await createWidget.mutateAsync({
      dashboardId,
      widgetType: type,
      col: 0,
      row: 0,
      width: 4,
      height: 4,
    });
    onSuccess();
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 py-4">
      <div className="space-y-2">
        <Label>Widget Type</Label>
        <Select value={type} onValueChange={(val) => setType(val as WidgetType)}>
          <SelectTrigger>
            <SelectValue placeholder="Select type" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={WidgetType.TaskList}>Task List</SelectItem>
            <SelectItem value={WidgetType.Metric}>Metric</SelectItem>
            <SelectItem value={WidgetType.Chart || "Chart"}>Chart</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <Button type="submit" className="w-full" disabled={createWidget.isPending}>
        {createWidget.isPending ? "Adding..." : "Add Widget"}
      </Button>
    </form>
  );
}
