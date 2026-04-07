import { Button } from "@/components/ui/button";
import { User, Shield, Bell, Globe, CreditCard } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";

export function SettingsSidebar() {
  const settingsItems = [
    { id: "general", icon: Globe, label: "General" },
    { id: "profile", icon: User, label: "Profile" },
    { id: "security", icon: Shield, label: "Security" },
    { id: "notifications", icon: Bell, label: "Notifications" },
    { id: "billing", icon: CreditCard, label: "Billing" },
  ];

  return (
    <ScrollArea className="flex-1 min-h-0">
      <div className="space-y-4 animate-in fade-in slide-in-from-left-1 duration-200">
        <div className="px-1 py-2">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            Workspace Settings
          </h3>
          <div className="space-y-1">
            {settingsItems.map((item) => {
              const Icon = item.icon;
              return (
                <Button
                  key={item.id}
                  variant="ghost"
                  className="w-full justify-start gap-3 h-10 px-3 hover:bg-accent/50 group"
                >
                  <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
                  <span className="text-sm font-medium">{item.label}</span>
                </Button>
              );
            })}
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}
