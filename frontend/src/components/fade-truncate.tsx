import * as React from "react";
import { cn } from "@/lib/utils";

type FadeTruncateProps = {
  text: string;
  className?: string;
  fadeWidth?: number;
  as?: "span" | "div";
};

export function FadeTruncate({
  text,
  className,
  fadeWidth = 12,
  as = "span",
}: FadeTruncateProps) {
  const Component = as;

  return (
    <Component
      className={cn("relative block min-w-0 w-full overflow-hidden", className)}
      title={text}
      style={
        {
          ["--label-bg" as any]: "var(--sidebar-bg, hsl(var(--background)))",
          ["--fade-width" as any]: `${fadeWidth}px`,
        } as React.CSSProperties
      }
    >
      <span className="block truncate">{text}</span>
      <span
        className="pointer-events-none absolute right-0 top-0 h-full bg-linear-to-l from-(--label-bg) to-transparent"
        style={{ width: "var(--fade-width)" }}
      />
    </Component>
  );
}
