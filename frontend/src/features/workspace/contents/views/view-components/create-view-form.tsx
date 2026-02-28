import { useState } from "react";
import { DialogFormWrapper } from "@/components/dialog-form-wrapper";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FieldLabel } from "@/components/ui/field";
import { ViewType } from "@/types/view-type";
import { useCreateView } from "../views-api";
import { toast } from "sonner";
import {
  Plus,
  LayoutList,
  Columns,
  LayoutDashboard,
  FileText,
  ArrowLeft,
  Loader2,
} from "lucide-react";

interface CreateViewFormProps {
  layerId: string;
  layerType: string;
}

const VIEW_OPTIONS = [
  {
    id: ViewType.List,
    label: "List",
    icon: LayoutList,
    desc: "Organize tasks in a structured list",
  },
  {
    id: ViewType.Board,
    label: "Board",
    icon: Columns,
    desc: "Track progress across columns",
  },
  {
    id: ViewType.Dashboard,
    label: "Dashboard",
    icon: LayoutDashboard,
    desc: "Visualize key metrics and data",
  },
  {
    id: ViewType.Doc,
    label: "Doc",
    icon: FileText,
    desc: "Create rich text documents",
  },
] as const;

export function CreateViewForm({ layerId, layerType }: CreateViewFormProps) {
  const [open, setOpen] = useState(false);
  const [step, setStep] = useState<1 | 2>(1);
  const [selectedType, setSelectedType] = useState<ViewType | null>(null);
  const [viewName, setViewName] = useState("");

  const createView = useCreateView();

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen);
    if (!newOpen) {
      // Reset state when closed
      setTimeout(() => {
        setStep(1);
        setSelectedType(null);
        setViewName("");
      }, 300);
    }
  };

  const handleTypeSelect = (type: ViewType) => {
    setSelectedType(type);
    setStep(2);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedType || !viewName.trim()) return;

    createView.mutate(
      { layerId, layerType, name: viewName.trim(), viewType: selectedType },
      {
        onSuccess: () => {
          toast.success("View created successfully");
          handleOpenChange(false);
        },
        onError: (err: any) => {
          // Extract title or errors array from RFC 9110 standard we saw in the logs
          const title = err.response?.data?.title;
          const detail = err.response?.data?.detail;
          toast.error(detail || title || "Failed to create view");
        },
      },
    );
  };

  return (
    <DialogFormWrapper
      open={open}
      onOpenChange={handleOpenChange}
      title={step === 1 ? "Choose View Type" : "Name your view"}
      contentClassName="sm:max-w-[425px]"
      trigger={
        <Button variant="ghost" size="sm" className="h-7 text-xs gap-1.5 px-2">
          <Plus className="h-3 w-3" />
          Add View
        </Button>
      }
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-4 py-4">
        {step === 1 ? (
          <div className="grid grid-cols-2 gap-3">
            {VIEW_OPTIONS.map((opt) => {
              const Icon = opt.icon;
              return (
                <button
                  key={opt.id}
                  type="button"
                  onClick={() => handleTypeSelect(opt.id)}
                  className="flex flex-col items-center justify-center gap-2 p-4 rounded-lg border-2 border-border bg-card hover:border-primary hover:bg-primary/5 transition-all text-center group"
                >
                  <div className="p-3 rounded-full bg-muted group-hover:bg-primary/10 transition-colors">
                    <Icon className="h-6 w-6 text-muted-foreground group-hover:text-primary transition-colors" />
                  </div>
                  <div>
                    <div className="font-semibold text-sm">{opt.label}</div>
                    <div className="text-[10px] text-muted-foreground leading-tight mt-1 px-2">
                      {opt.desc}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        ) : (
          <div className="space-y-4 animate-in slide-in-from-right-2 duration-300">
            <Field>
              <FieldLabel className="text-xs uppercase tracking-wider font-mono text-muted-foreground">
                View Name
              </FieldLabel>
              <Input
                autoFocus
                placeholder="e.g. Q3 Roadmap, Bug Tracker..."
                value={viewName}
                onChange={(e) => setViewName(e.target.value)}
                disabled={createView.isPending}
                className="font-medium"
              />
            </Field>

            <div className="flex gap-2 justify-end pt-4">
              <Button
                type="button"
                variant="ghost"
                onClick={() => setStep(1)}
                disabled={createView.isPending}
                className="gap-2"
              >
                <ArrowLeft className="h-4 w-4" />
                Back
              </Button>
              <Button
                type="submit"
                disabled={!viewName.trim() || createView.isPending}
              >
                {createView.isPending && (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                )}
                Create View
              </Button>
            </div>
          </div>
        )}
      </form>
    </DialogFormWrapper>
  );
}
