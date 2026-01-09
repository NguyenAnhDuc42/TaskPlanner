"use client";

import * as React from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import { Plus } from "lucide-react";
import type { WorkspaceVariant } from "@/types/workspace-variant";
import type { Theme } from "@/types/theme";
import { ColorPicker } from "@/components/color-picker";
import IconPicker from "@/components/icon-picker";
import { ScrollArea } from "@/components/ui/scroll-area";

const VARIANTS: WorkspaceVariant[] = ["Personal", "Team", "Company"];
const THEMES: Theme[] = ["Light", "Dark", "System"];

export type FormData = {
  name: string;
  description: string;
  variant: WorkspaceVariant;
  theme: Theme;
  color: string;
  icon: string;
};

type Props = {
  onSubmit?: (data: FormData) => void;
  isLoading?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  showTrigger?: boolean;
};

export function CreateWorkspaceForm({
  onSubmit,
  isLoading,
  open: controlledOpen,
  onOpenChange: controlledOnOpenChange,
  showTrigger = true,
}: Props) {
  const [uncontrolledOpen, setUncontrolledOpen] = React.useState(false);

  const open = controlledOpen ?? uncontrolledOpen;
  const setOpen = controlledOnOpenChange ?? setUncontrolledOpen;

  const [data, setData] = React.useState<FormData>({
    name: "",
    description: "",
    variant: "Personal",
    theme: "System",
    color: "#4f46e5",
    icon: "AppWindow",
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!data.name.trim() || isLoading) return;

    onSubmit?.(data);

    // Reseting state only if not controlled from outside might be better,
    // but here we follow the current pattern.
    setData({
      name: "",
      description: "",
      variant: "Personal",
      theme: "System",
      color: "#4f46e5",
      icon: "AppWindow",
    });
    setOpen(false);
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      {showTrigger && (
        <DialogTrigger asChild>
          <Button
            disabled={isLoading}
            className="flex items-center gap-2 h-9 px-4 bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono text-sm"
          >
            <Plus className="h-4 w-4" />
            Create
          </Button>
        </DialogTrigger>
      )}

      <DialogContent className="max-w-md p-0 overflow-hidden flex flex-col max-h-[90vh]">
        <DialogHeader className="p-6 pb-0">
          <DialogTitle>Create Workspace</DialogTitle>
          <DialogDescription>
            Set up a new workspace for your team
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="flex-1 flex flex-col min-h-0"
          noValidate
        >
          <ScrollArea className="flex-1 px-6">
            <div className="space-y-6 py-6">
              <FieldGroup>
                {/* Name */}
                <Field>
                  <FieldLabel htmlFor="name">Workspace Name</FieldLabel>
                  <Input
                    id="name"
                    placeholder="My Workspace"
                    value={data.name}
                    onChange={(e) => setData({ ...data, name: e.target.value })}
                    required
                    disabled={isLoading}
                  />
                </Field>

                {/* Description */}
                <Field>
                  <FieldLabel htmlFor="description">
                    Description (Optional)
                  </FieldLabel>
                  <Textarea
                    id="description"
                    placeholder="Describe your workspace..."
                    value={data.description}
                    onChange={(e) =>
                      setData({ ...data, description: e.target.value })
                    }
                    rows={2}
                    disabled={isLoading}
                  />
                </Field>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <Field>
                    <FieldLabel>Workspace Color</FieldLabel>
                    <ColorPicker
                      value={data.color}
                      onChange={(color: string) => setData({ ...data, color })}
                    />
                  </Field>

                  <Field>
                    <FieldLabel>Workspace Icon</FieldLabel>
                    <IconPicker
                      value={data.icon}
                      onChange={(icon: string) => setData({ ...data, icon })}
                    />
                  </Field>
                </div>

                {/* Variant - Tab-like Radio Group */}
                <Field>
                  <FieldLabel>Workspace Type</FieldLabel>
                  <RadioGroup
                    value={data.variant}
                    disabled={isLoading}
                    onValueChange={(variant) =>
                      setData({ ...data, variant: variant as WorkspaceVariant })
                    }
                    className="grid grid-cols-3 gap-2"
                  >
                    {VARIANTS.map((variant) => (
                      <label
                        key={variant}
                        className={cn(
                          "flex items-center justify-center px-4 py-2 border border-border rounded cursor-pointer transition-all font-mono text-xs",
                          data.variant === variant
                            ? "bg-primary text-primary-foreground border-primary"
                            : "bg-card hover:bg-card/80 text-foreground"
                        )}
                      >
                        <RadioGroupItem value={variant} className="sr-only" />
                        {variant}
                      </label>
                    ))}
                  </RadioGroup>
                </Field>

                {/* Theme - Tab-like Radio Group */}
                <Field>
                  <FieldLabel>Theme Preference</FieldLabel>
                  <RadioGroup
                    value={data.theme}
                    disabled={isLoading}
                    onValueChange={(theme) =>
                      setData({ ...data, theme: theme as Theme })
                    }
                    className="grid grid-cols-3 gap-2"
                  >
                    {THEMES.map((theme) => (
                      <label
                        key={theme}
                        className={cn(
                          "flex items-center justify-center px-4 py-2 border border-border rounded cursor-pointer transition-all font-mono text-xs",
                          data.theme === theme
                            ? "bg-primary text-primary-foreground border-primary"
                            : "bg-card hover:bg-card/80 text-foreground"
                        )}
                      >
                        <RadioGroupItem value={theme} className="sr-only" />
                        {theme}
                      </label>
                    ))}
                  </RadioGroup>
                </Field>
              </FieldGroup>
            </div>
          </ScrollArea>

          <div className="p-6 pt-2 border-t border-border bg-card">
            <Button
              type="submit"
              disabled={!data.name.trim() || isLoading}
              className="w-full bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono"
            >
              {isLoading ? "Creating..." : "Create Workspace"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
