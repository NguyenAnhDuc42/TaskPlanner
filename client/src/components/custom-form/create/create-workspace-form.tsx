import { ColorPicker } from "@/components/custom/color-picker";
import { IconPicker } from "@/components/custom/icon-picker";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { CreateWorkspaceRequest } from "@/features/workspace/workspacetype";
import { useState } from "react";
import { useCreateWorkspace } from "@/features/user/user-hooks";
import { DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";

interface CreateWorkspaceFormProps {
  onCancel: () => void;
  onSuccess?: () => void; 
}

export function CreateWorkspaceForm({
  onCancel,
  onSuccess,
}: CreateWorkspaceFormProps) {
  const { mutate: createWorkspaceMutation, isPending } = useCreateWorkspace();

  const [formData, setFormData] =
    useState<CreateWorkspaceRequest>({
      name: "",
      description: "",
      icon: "Building2",
      color: "#ffffff",
      isPrivate: false,
    });

  const [errors, setErrors] = useState<Partial<CreateWorkspaceRequest>>({});

  const validateForm = () => {
    const newErrors: Partial<CreateWorkspaceRequest> = {};
    if (!formData.name.trim()) {
      newErrors.name = "Workspace name is required";
    } else if (formData.name.trim().length < 2) {
      newErrors.name =
        "Workspace name must be at least 2 characters";
    }

    if (!formData.description.trim()) {
      newErrors.description = "Description is required";
    } else if (formData.description.trim().length < 10) {
      newErrors.description =
        "Description must be at least 10 characters";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (validateForm()) {
      createWorkspaceMutation(
        {
          ...formData,
          name: formData.name.trim(),
          description: formData.description.trim(),
        },
        {
          onSuccess: () => {
            onSuccess?.();
          },
        }
      );
    }
  };

  const updateFormData = <K extends keyof CreateWorkspaceRequest,>(
    key: K, 
    value: CreateWorkspaceRequest[K],
  ) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
    if (errors[key]) {
      setErrors((prev) => ({ ...prev, [key]: undefined }));
    }
  };

  return (
    <DialogContent className="sm:max-w-[550px] max-h-[90vh] overflow-y-auto bg-card border-border" style={{ width: '550px', maxWidth: '90vw' }}>
      <DialogHeader>
        <DialogTitle className="text-foreground">Create New Workspace</DialogTitle>
        <p className="text-muted-foreground text-sm">
          Set up a new workspace for your team to collaborate and organize projects.
        </p>
      </DialogHeader>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Workspace Name */}
        <div className="space-y-2">
          <Label htmlFor="name" className="text-foreground">Workspace Name *</Label>
          <Input 
            id="name" 
            value={formData.name} 
            onChange={(e) => updateFormData("name", e.target.value)}
            placeholder="Enter workspace name..."
            className="w-full bg-background border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:ring-primary"
            disabled={isPending}
            maxLength={100}
          />
          {errors.name && (
            <p className="text-destructive text-sm break-words">{errors.name}</p>
          )}
        </div>

        {/* Description */}
        <div className="space-y-2">
          <Label htmlFor="description" className="text-foreground">Description *</Label>
          <div className="relative">
            <Textarea 
              id="description" 
              value={formData.description} 
              onChange={(e) => updateFormData("description", e.target.value)}
              placeholder="Describe what this workspace is for..."
              className="w-full bg-background border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:ring-primary min-h-20 max-h-32 resize-none !whitespace-pre-wrap !break-words !overflow-wrap-break-word"
              disabled={isPending}
              maxLength={500}
              rows={3}
              wrap="soft"
              style={{
                wordWrap: 'break-word',
                overflowWrap: 'break-word',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                hyphens: 'auto',
                width: '100%',
                maxWidth: '100%',
                boxSizing: 'border-box'
              }}
            />
          </div>
          <div className="flex justify-between items-center">
            {errors.description && (
              <p className="text-destructive text-sm break-words flex-1">{errors.description}</p>
            )}
            <p className="text-muted-foreground text-xs ml-auto">
              {formData.description.length}/500
            </p>
          </div>
        </div>

        <Separator className="bg-border" />

        {/* Icon Selection */}
        <div className="space-y-3">
          <Label className="text-foreground">Workspace Icon</Label>
          <div className="w-full overflow-hidden">
            <IconPicker
              value={formData.icon}
              onChange={(icon) => updateFormData("icon", icon)}
            />
          </div>
        </div>

        {/* Color Selection */}
        <div className="space-y-3">
          <Label className="text-foreground">Workspace Color</Label>
          <div className="w-full overflow-hidden">
            <ColorPicker
              value={formData.color}
              onChange={(color) => updateFormData("color", color)}
            />
          </div>
        </div>

        <Separator className="bg-border" />

        {/* Privacy Setting */}
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-1 flex-1 min-w-0">
            <Label className="text-foreground">
              Private Workspace
            </Label>
            <p className="text-sm text-muted-foreground break-words">
              Only invited members can see and join this workspace
            </p>
          </div>
          <div className="flex-shrink-0">
            <Switch
              checked={formData.isPrivate}
              onCheckedChange={(checked) =>
                updateFormData("isPrivate", checked)
              }
              disabled={isPending} 
            />
          </div>
        </div>

        {/* Form Actions */}
        <div className="flex flex-col sm:flex-row justify-end gap-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={isPending}
            className="w-full sm:w-auto"
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={isPending}
            className="w-full sm:w-auto"
          >
            {isPending ? "Creating..." : "Create Workspace"}
          </Button>
        </div>
      </form>
    </DialogContent>
  );
}