import { Button } from "@/components/ui/button";
import { Link, useParams } from "@tanstack/react-router";
import { MessageCircle, User } from "lucide-react";

export function MembersSidebar() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });

  const settingsItems = [
    { id: "members", icon: User, label: "Members" },
    { id: "communications", icon: MessageCircle, label: "Communication" },
  ];

  return (
    <div className="space-y-4 animate-in fade-in slide-in-from-left-1 duration-200">
      <div className="px-1 py-2">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
          Workspace Members
        </h3>
        <div className="space-y-1">
          {settingsItems.map((item) => {
            const Icon = item.icon;
            return (
              <Button
                key={item.id}
                variant="ghost"
                className="w-full justify-start gap-3 h-10 px-3 hover:bg-accent/50 group"
                asChild
              >
                <Link to={"/"+"workspace"+ "/" + workspaceId + "/" + item.id}>
                  <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
                  <span className="text-sm font-medium">{item.label}</span>
                </Link>
              </Button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
