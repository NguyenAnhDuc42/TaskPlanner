---
description: Audit and optimize the frontend for real performance/correctness issues, root-cause style
---

Audit and optimize the frontend (`frontend/src/`). Follow this approach exactly — it's the style that's worked before, deviations have not:

1. **Read the code deeply and diagnose the mechanism before changing anything.** Don't ask for repro steps or benchmarks first — read the code. Treat symptom descriptions (even vague or typo'd ones) as high-signal: "slower each time" means a compounding cost, "click doesn't register sometimes" means an intermittent mechanism, not user error.
2. **Fix root causes, not mitigations.** A patch that hides a symptom without explaining *why* it was happening is not an acceptable fix.
3. **Prioritize compounding costs over constant costs** — things that get worse as data grows or navigation count rises matter more than fixed one-time costs.
4. **Explain the mechanism when reporting** — why something was slow/leaky/broken, not just what changed.
5. **Preserve established store patterns** unless the audit itself is about them: shallow observable maps (`deep: false`, records replaced wholesale) and private computed group-by indexes returning shared arrays (callers must copy before mutating).
6. Read header comments on load-bearing files before touching them — some carry documented bug history (e.g. `document-editor-host.tsx`, `use-block-editor-sync.ts`).
7. **Verify with `npx tsc -b --force` (from `frontend/`) + eslint on touched files** before reporting anything as done.

Past sessions in this style found: a persistent-editor memory leak (context+portal pattern), unbatched MobX upserts, deep-observable overhead, full-store-scan accessors (fixed via computed indexes), and a dnd-kit 5px activation constraint that was eating clicks. Look for the same class of issue: things that silently get worse, not just things that look inefficient in isolation.
