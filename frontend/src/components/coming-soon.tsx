import { Construction, Sparkles } from "lucide-react";
import { Button } from "./ui/button";

interface ComingSoonProps {
  title: string;
  description: string;
}

export function ComingSoon({ title, description }: ComingSoonProps) {
  return (
    <div className="flex flex-col items-center justify-center h-full w-full bg-transparent p-8 text-center animate-in fade-in zoom-in duration-500">
      <div className="relative mb-6">
        <div className="absolute -inset-4 bg-primary/20 blur-xl rounded-full opacity-50" />
        <div className="relative bg-background border border-border/50 h-20 w-20 rounded-2xl flex items-center justify-center shadow-sm">
          <Construction className="h-10 w-10 text-primary/80" />
          <Sparkles className="absolute -top-2 -right-2 h-6 w-6 text-yellow-500 animate-pulse" />
        </div>
      </div>
      
      <h2 className="text-2xl font-bold tracking-tight mb-2 text-foreground">
        {title} <span className="text-muted-foreground font-medium ml-1">is coming soon</span>
      </h2>
      
      <p className="text-muted-foreground max-w-md mx-auto mb-8 text-sm leading-relaxed">
        {description}
      </p>

      <Button variant="outline" className="gap-2" onClick={() => window.history.back()}>
        Go Back
      </Button>
    </div>
  );
}
