# Frontend Architecture & Workflow Guide

This document establishes the "Gold Standard" for frontend development in the TaskPlanner codebase. All new features and refactors must adhere to these patterns.

## 1. Feature Folder Structure
Each feature in `src/features/` must follow this hierarchy:

```
src/features/[feature-name]/
├── [feature-name]-index.tsx      # Main entry point or page component
├── [feature-name]-sidebar.tsx    # Feature-specific sidebar (if applicable)
├── [feature-name]-api.ts         # Axios calls AND TanStack Query Hooks
├── [feature-name]-keys.ts        # Query key factory
├── [feature-name]-type.ts        # TypeScript interfaces, types, and ENUMS
└── [feature-name]-components/    # Sub-components used only by this feature
    ├── component-a.tsx
    └── component-b.tsx
```

> [!IMPORTANT]
> Avoid "flat" folders. If a component is not the main index or sidebar, it **must** live in the `[feature-name]-components/` subfolder.

## 2. API & Logic Management
*   **Unified Files**: Keep the `mutationFn`/`queryFn` and the resulting `useQuery`/`useMutation` hook in the same `*-api.ts` file.
*   **Query Keys**: Always use the central Query Key factory from `*-keys.ts` to ensure consistency across invalidations.

## 3. Strong Typing & Enums
*   **No Raw Strings**: Never use raw strings for constants that represent categories, types, or statuses. Use `enums` or `as const` objects defined in `*-type.ts` and in types folder.
*   **Prop Types**: Prefer interfaces/types for component props over inline definitions.

## 4. Data Fetching Pattern (Loaders)
*   **TanStack Router Loaders**: Prefer pre-fetching critical data in the route definition using TanStack Query's `ensureQueryData`.
*   **Component Usage**: Use the pre-fetched data in the component to avoid "Loading..." flickers on initial navigation.

```typescript
// Example Route Loader
export const Route = createFileRoute('/workspaces/$workspaceId')({
  loader: async ({ context: { queryClient }, params: { workspaceId } }) => {
    await queryClient.ensureQueryData(workspaceKeys.detail(workspaceId));
  },
  component: WorkspaceLayout,
});
```

## 5. Visual Consistency
*   **Glass panels**: Use the `.glass-panel` utility for all containers.
*   **Theme Tokens**: Always use CSS variables (`--theme-text-normal`, `--theme-item-hover`, etc.) instead of Tailwind's default colors.
