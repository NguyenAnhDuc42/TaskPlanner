
import { cn } from "@/lib/utils";

type FadeTruncateProps = {
  text: string;
  className?: string;
  as?: "span" | "div";
};

export function FadeTruncate({
  text,
  className,
  as = "span",
}: FadeTruncateProps) {
  const Component = as;

  return (
    <Component
      className={cn("block truncate min-w-0 w-full", className)}
      title={text}
    >
      {text}
    </Component>
  );
}
