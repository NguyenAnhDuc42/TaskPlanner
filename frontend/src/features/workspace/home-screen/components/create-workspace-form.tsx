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

const VARIANTS: WorkspaceVariant[] = ["Personal", "Team", "Company"];
const THEMES: Theme[] = ["Light", "Dark", "System"];

type FormData = {
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
    color: "#ffffffff",
    icon: "AppWindow",
  });

  // ... (rest of the component remains the same, but using the synchronized 'open' and 'setOpen')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit?.(data);
    setData({
      name: "",
      description: "",
      variant: "Personal",
      theme: "System",
      color: "#ffffffff",
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

      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Workspace</DialogTitle>
          <DialogDescription>
            Set up a new workspace for your team
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6" noValidate>
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
                rows={3}
              />
            </Field>

            <Field>
              <FieldLabel>Workspace Color</FieldLabel>
              <ColorPicker
                value={data.color}
                onChange={(color) => setData({ ...data, color })}
              />
            </Field>

            <Field>
              <FieldLabel>Workspace Icon</FieldLabel>
              <IconPicker
                value={data.icon}
                onChange={(icon) => setData({ ...data, icon })}
              />
            </Field>

            {/* Variant - Tab-like Radio Group */}
            <Field>
              <FieldLabel>Workspace Type</FieldLabel>
              <RadioGroup
                value={data.variant}
                onValueChange={(variant) =>
                  setData({ ...data, variant: variant as WorkspaceVariant })
                }
                className="grid grid-cols-3 gap-2"
              >
                {VARIANTS.map((variant) => (
                  <label
                    key={variant}
                    className={cn(
                      "flex items-center justify-center px-4 py-2 border border-border rounded cursor-pointer transition-all font-mono text-sm",
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
              <FieldLabel>Theme</FieldLabel>
              <RadioGroup
                value={data.theme}
                onValueChange={(theme) =>
                  setData({ ...data, theme: theme as Theme })
                }
                className="grid grid-cols-3 gap-2"
              >
                {THEMES.map((theme) => (
                  <label
                    key={theme}
                    className={cn(
                      "flex items-center justify-center px-4 py-2 border border-border rounded cursor-pointer transition-all font-mono text-sm",
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

          <Button
            type="submit"
            disabled={!data.name.trim() || isLoading}
            className="w-full bg-primary hover:bg-primary/90 text-primary-foreground border-0 font-mono"
          >
            {isLoading ? "Creating..." : "Create Workspace"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}
