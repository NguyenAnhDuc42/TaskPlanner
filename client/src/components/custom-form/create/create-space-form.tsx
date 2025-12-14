import { ColorPicker } from "@/components/custom/color-picker";
import { IconPicker } from "@/components/custom/icon-picker";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import { useCreateSpace } from "@/features/space/space-hooks";
import { CreateSpaceBody } from "@/features/space/space-type";
import { useState } from "react";

interface CreateSpaceFormProps {
  workspaceId: string;
  onCancel: () => void;
  onSuccess?: () => void;
}



export function CreateSpaceForm({
  workspaceId,
  onCancel,
  onSuccess,
}: CreateSpaceFormProps) {
  const { mutate: createSpaceMutation, isPending } = useCreateSpace();

  const [formData, setFormData] = useState<CreateSpaceBody>({
    name: "",
    icon: "Building2",
    color: "#3b82f6",
  });

  const [errors, setErrors] = useState<Partial<CreateSpaceBody>>({});

  const validateForm = () => {
    const newErrors: Partial<CreateSpaceBody> = {};
    
    if (!formData.name.trim()) {
      newErrors.name = "Space name is required";
    } else if (formData.name.trim().length < 2) {
      newErrors.name = "Space name must be at least 2 characters";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (validateForm()) {
      const createSpaceBody: CreateSpaceBody = {
        name: formData.name.trim(),
        icon: formData.icon,
        color: formData.color,
      };

      createSpaceMutation(
        { workspaceId, body: createSpaceBody },
        {
          onSuccess: () => {
            onSuccess?.();
          },
        }
      );
    }
  };

  const updateFormData = <K extends keyof CreateSpaceBody>(
    key: K,
    value: CreateSpaceBody[K]
  ) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
    if (errors[key]) {
      setErrors((prev) => ({ ...prev, [key]: undefined }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Icon & Color Selection - Side by Side */}
      <div className="space-y-3">
        <Label className="text-foreground">Icon & Color</Label>
        <div className="grid grid-cols-2 gap-3">
          <IconPicker
            value={formData.icon}
            onChange={(icon) => updateFormData("icon", icon)}
          />
          <ColorPicker
            value={formData.color}
            onChange={(color) => updateFormData("color", color)}
          />
        </div>
      </div>

      {/* Space Name */}
      <div className="space-y-2">
        <Label className="text-foreground">Space Name</Label>
        <Input
          value={formData.name}
          onChange={(e) => updateFormData("name", e.target.value)}
          placeholder="e.g. Marketing, Engineering, HR"
          className="w-full bg-background border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:ring-primary"
          disabled={isPending}
          maxLength={100}
        />
        {errors.name && (
          <p className="text-destructive text-sm">{errors.name}</p>
        )}
      </div>

      <Separator className="bg-border" />

      {/* Privacy Setting */}
      <div className="flex items-start justify-between gap-4">
        <div className="space-y-1 flex-1">
          <Label className="text-foreground">Make Private</Label>
          <p className="text-sm text-muted-foreground">
            Only you and invited members have access
          </p>
        </div>
        <div className="flex-shrink-0">
          <Switch
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
          {isPending ? "Creating..." : "Continue"}
        </Button>
      </div>
    </form>
  );
}