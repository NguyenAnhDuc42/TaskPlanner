import { lazy } from "react";

export const LazyBlockEditor = lazy(() =>
  import("./block-editor").then((m) => ({ default: m.BlockEditor })),
);
