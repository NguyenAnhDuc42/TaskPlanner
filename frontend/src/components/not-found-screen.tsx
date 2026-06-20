import { ShieldAlert, ArrowLeft } from "lucide-react";
import { Button } from "./ui/button";
import { useNavigate, useParams } from "@tanstack/react-router";

interface NotFoundScreenProps {
  title?: string;
  description?: string;
}

export function NotFoundScreen({ 
  title = "Access Denied or Not Found", 
  description = "The resource you are trying to access might have been deleted, or you don't have the required permissions to view it." 
}: NotFoundScreenProps) {
  const navigate = useNavigate();
  const { workspaceId } = useParams({ strict: false });

  return (
    <div className="flex flex-col items-center justify-center h-full w-full bg-transparent p-8 text-center animate-in fade-in zoom-in duration-500">
      <div className="relative mb-6">
        <div className="absolute -inset-4 bg-destructive/20 blur-xl rounded-full opacity-50" />
        <div className="relative bg-background border border-border/50 h-20 w-20 rounded-2xl flex items-center justify-center shadow-sm">
          <ShieldAlert className="h-10 w-10 text-destructive/80" />
        </div>
      </div>
      
      <h2 className="text-2xl font-bold tracking-tight mb-2 text-foreground">
        {title}
      </h2>
      
      <p className="text-muted-foreground max-w-md mx-auto mb-8 text-sm leading-relaxed">
        {description}
      </p>

      <Button 
        variant="outline" 
        className="gap-2" 
        onClick={() => {
          if (workspaceId) {
            navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
          } else {
            window.history.back();
          }
        }}
      >
        <ArrowLeft className="h-4 w-4" />
        Go Back to Workspace
      </Button>
    </div>
  );
}
