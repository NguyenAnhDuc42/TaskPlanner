import { createFileRoute } from "@tanstack/react-router";
import { useState, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { RootStore } from "@/stores/root.store";
import { WorkspaceRootStore } from "@/stores/workspace-root.store";
import { SyncEngine } from "@/sync/sync-engine";
import { TaskMutations } from "@/mutations/task.mutations";
import { SpaceMutations } from "@/mutations/space.mutations";
import { FolderMutations } from "@/mutations/folder.mutations";
import { WorkspaceMutations } from "@/mutations/workspace.mutations";
import type { WorkspaceRecord } from "@/types/workspace/workspace-record";
import { api } from "@/lib/api-client";

const row: React.CSSProperties = {
  display: "flex",
  flexWrap: "wrap",
  alignItems: "center",
  gap: 8,
  border: "1px solid #444",
  borderRadius: 4,
  padding: 8,
};

const section: React.CSSProperties = {
  marginBottom: 24,
  borderTop: "1px solid #333",
  paddingTop: 12,
};

const SyncTestPage = observer(function SyncTestPage() {
  const rootStore = useMemo(() => new RootStore(), []);
  const workspaceMutations = useMemo(() => new WorkspaceMutations(rootStore), [rootStore]);

  // Workspace-scope store — a fresh instance per connect/switch, not a mutated shared one.
  const [workspaceRootStore, setWorkspaceRootStore] = useState<WorkspaceRootStore | null>(null);
  const syncEngine = useMemo(() => (workspaceRootStore ? new SyncEngine(workspaceRootStore) : null), [workspaceRootStore]);
  const taskMutations = useMemo(() => (workspaceRootStore && syncEngine ? new TaskMutations(workspaceRootStore, syncEngine) : null), [workspaceRootStore, syncEngine]);
  const spaceMutations = useMemo(() => (workspaceRootStore && syncEngine ? new SpaceMutations(workspaceRootStore, syncEngine) : null), [workspaceRootStore, syncEngine]);
  const folderMutations = useMemo(() => (workspaceRootStore && syncEngine ? new FolderMutations(workspaceRootStore, syncEngine) : null), [workspaceRootStore, syncEngine]);

  const [workspaceId, setWorkspaceId] = useState("9cb7f2f3-41e6-4dce-bb16-a126f3a0b908");
  const [activeWorkspaceId, setActiveWorkspaceId] = useState("9cb7f2f3-41e6-4dce-bb16-a126f3a0b908");
  const [connected, setConnected] = useState(false);
  const [workspaceList, setWorkspaceList] = useState<WorkspaceRecord[]>([]);
  const [wsCreateName, setWsCreateName] = useState("");

  const [spaceName, setSpaceName] = useState("");
  const [folderName, setFolderName] = useState("");
  const [folderSpaceId, setFolderSpaceId] = useState("");
  const [taskName, setTaskName] = useState("");
  const [taskSpaceId, setTaskSpaceId] = useState("");
  const [taskFolderId, setTaskFolderId] = useState("");

  const [editValues, setEditValues] = useState<Record<string, string>>({});
  const [log, setLog] = useState<string[]>([]);

  const addLog = (msg: string) => setLog((prev) => [`${new Date().toLocaleTimeString()} — ${msg}`, ...prev]);
  const setEdit = (id: string, val: string) => setEditValues((prev) => ({ ...prev, [id]: val }));

  // ── Connection ──
  const enterWorkspace = async (id: string): Promise<{ wrs: WorkspaceRootStore; engine: SyncEngine }> => {
    workspaceRootStore?.dispose();
    const wrs = new WorkspaceRootStore(id);
    await wrs.hydrate();
    const engine = new SyncEngine(wrs);
    await engine.init(id);
    setWorkspaceRootStore(wrs);
    return { wrs, engine };
  };

  const handleConnect = async () => {
    if (!workspaceId) return;
    try {
      addLog(`Switching to workspace ${workspaceId}...`);
      const { wrs } = await enterWorkspace(workspaceId);
      addLog("IndexedDB hydrated. Sync engine connected.");
      setConnected(true);
      setActiveWorkspaceId(workspaceId);
      addLog("lastSyncId in metadata: " + (await wrs.metadataDB?.getLastSyncId()));
      addLog(
        `Bootstrap counts — tasks: ${wrs.taskStore.all.length}, spaces: ${wrs.spaceStore.all.length}, folders: ${wrs.folderStore.all.length}, statuses: ${wrs.statusStore.all.length}`
      );
      const wsRes = await api.get("/workspaces?pageSize=50");
      const items: WorkspaceRecord[] = wsRes.data?.items ?? [];
      setWorkspaceList(items);
      for (const w of items) rootStore.workspaceStore.upsert(w as never);
      addLog(`Workspaces fetched: ${items.length}`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleFlushQueue = async () => {
    if (!syncEngine) return;
    addLog("Flushing pending transaction queue...");
    await syncEngine.flushQueue();
    addLog("Flush complete.");
  };

  const handleForceBootstrap = async () => {
    if (!syncEngine || !workspaceRootStore) return;
    try {
      addLog("Force re-bootstrap (resetting lastSyncId → 0)...");
      await syncEngine.forceBootstrap(activeWorkspaceId);
      addLog(
        `Re-bootstrap done — tasks: ${workspaceRootStore.taskStore.all.length}, spaces: ${workspaceRootStore.spaceStore.all.length}, folders: ${workspaceRootStore.folderStore.all.length}, statuses: ${workspaceRootStore.statusStore.all.length}`
      );
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  // ── Workspace ──
  const handleSwitchWorkspace = async (newId: string) => {
    if (newId === activeWorkspaceId) return;
    try {
      addLog(`Switching to workspace ${newId}...`);
      const { wrs } = await enterWorkspace(newId);
      setActiveWorkspaceId(newId);
      setWorkspaceId(newId);
      addLog("Connected. lastSyncId: " + (await wrs.metadataDB?.getLastSyncId()));
      addLog(
        `Bootstrap — tasks: ${wrs.taskStore.all.length}, spaces: ${wrs.spaceStore.all.length}, folders: ${wrs.folderStore.all.length}, statuses: ${wrs.statusStore.all.length}`
      );
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleCreateWorkspace = async () => {
    if (!wsCreateName) return;
    try {
      addLog(`Creating workspace "${wsCreateName}" (server-first)...`);
      const record = await workspaceMutations.create({ name: wsCreateName });
      setWorkspaceList((prev) => [...prev, record]);
      setWsCreateName("");
      addLog(`Workspace created: ${record.id} — click Switch to connect once server finishes initializing.`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleUpdateWorkspace = async (id: string) => {
    const newName = editValues[id];
    if (!newName) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Updating workspace ${id} → name="${newName}" (isOnline=${wasOnline})...`);
      await workspaceMutations.update(id, { name: newName });
      setWorkspaceList((prev) => prev.map((w) => (w.id === id ? { ...w, name: newName } : w)));
      setEdit(id, "");
      addLog(wasOnline ? `Update sent. Waiting for Delta confirmation...` : `Update queued — will sync when online.`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  // ── Spaces ──
  const handleCreateSpace = async () => {
    if (!spaceName || !spaceMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Creating space "${spaceName}" (isOnline=${wasOnline})...`);
      const record = await spaceMutations.create({ name: spaceName, isPrivate: false });
      addLog(
        wasOnline
          ? `Space created locally with id ${record.id}. Waiting for DeltaBatch confirmation (Space + 4 Statuses)...`
          : `Space created locally with id ${record.id}. Queued — will send when back online.`
      );
      setSpaceName("");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleUpdateSpace = async (id: string) => {
    const newName = editValues[id];
    if (!newName || !spaceMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Updating space ${id} → name="${newName}" (isOnline=${wasOnline})...`);
      await spaceMutations.update(id, { name: newName });
      addLog(wasOnline ? `Update sent. Waiting for Delta confirmation...` : `Update queued — will sync when online.`);
      setEdit(id, "");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleDeleteSpace = async (id: string) => {
    if (!spaceMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Deleting space ${id} (isOnline=${wasOnline})...`);
      await spaceMutations.delete(id);
      addLog(wasOnline ? `Delete sent (removed locally immediately).` : `Delete queued — will sync when online.`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  // ── Folders ──
  const handleCreateFolder = async () => {
    if (!folderName || !folderSpaceId || !folderMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Creating folder "${folderName}" in space ${folderSpaceId} (isOnline=${wasOnline})...`);
      const record = await folderMutations.create({ name: folderName, spaceId: folderSpaceId });
      addLog(
        wasOnline
          ? `Folder created locally with id ${record.id}. Waiting for Delta confirmation...`
          : `Folder created locally with id ${record.id}. Queued — will send when back online.`
      );
      setFolderName("");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleUpdateFolder = async (id: string) => {
    const newName = editValues[id];
    if (!newName || !folderMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Updating folder ${id} → name="${newName}" (isOnline=${wasOnline})...`);
      await folderMutations.update(id, { name: newName });
      addLog(wasOnline ? `Update sent. Waiting for Delta confirmation...` : `Update queued — will sync when online.`);
      setEdit(id, "");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleDeleteFolder = async (id: string) => {
    if (!folderMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Deleting folder ${id} (isOnline=${wasOnline})...`);
      await folderMutations.delete(id);
      addLog(wasOnline ? `Delete sent (removed locally immediately).` : `Delete queued — will sync when online.`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  // ── Tasks ──
  const handleCreateTask = async () => {
    if (!taskName || !taskMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Creating task "${taskName}" (isOnline=${wasOnline})...`);
      const record = await taskMutations.create({ name: taskName, spaceId: taskSpaceId, folderId: taskFolderId || null });
      addLog(
        wasOnline
          ? `Task created locally with id ${record.id}. Waiting for Delta confirmation...`
          : `Task created locally with id ${record.id}. Queued — will send when back online.`
      );
      setTaskName("");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleUpdateTask = async (id: string) => {
    const newName = editValues[id];
    if (!newName || !taskMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Updating task ${id} → name="${newName}" (isOnline=${wasOnline})...`);
      await taskMutations.update(id, { name: newName });
      addLog(wasOnline ? `Update sent. Waiting for Delta confirmation...` : `Update queued — will sync when online.`);
      setEdit(id, "");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleDeleteTask = async (id: string) => {
    if (!taskMutations) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Deleting task ${id} (isOnline=${wasOnline})...`);
      await taskMutations.delete(id);
      addLog(wasOnline ? `Delete sent. Waiting for Delta confirmation...` : `Delete queued — will sync when online.`);
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  return (
    <div style={{ padding: 24, fontFamily: "monospace", maxWidth: 1000 }}>
      <h2>Sync Engine Test Page</h2>

      {/* ── WORKSPACE ── */}
      <div style={section}>
        <h3>Workspaces</h3>

        <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 12 }}>
          <input
            placeholder="Workspace ID"
            value={workspaceId}
            onChange={(e) => setWorkspaceId(e.target.value)}
            style={{ width: 300 }}
            disabled={connected}
          />
          <button onClick={handleConnect} disabled={connected || !workspaceId}>
            {connected ? "Connected" : "Connect"}
          </button>
          {connected && (
            <>
              <button
                onClick={() => {
                  const next = !rootStore.isOnline;
                  rootStore.setOnline(next);
                  addLog(`isOnline manually set to ${next}`);
                }}
              >
                {rootStore.isOnline ? "Go Offline" : "Go Online"}
              </button>
              <span style={{ color: rootStore.isOnline ? "lightgreen" : "salmon" }}>
                {rootStore.isOnline ? "● online" : "● offline"}
              </span>
              <button onClick={handleFlushQueue}>Flush Queue</button>
              <button onClick={handleForceBootstrap}>Force Re-Bootstrap</button>
            </>
          )}
        </div>

        {connected && (
          <>
            <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
              <input
                placeholder="New workspace name"
                value={wsCreateName}
                onChange={(e) => setWsCreateName(e.target.value)}
                style={{ width: 240 }}
              />
              <button onClick={handleCreateWorkspace} disabled={!wsCreateName}>
                Create Workspace
              </button>
            </div>

            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {workspaceList.map((w) => (
                <div
                  key={w.id}
                  style={{
                    ...row,
                    borderColor: w.id === activeWorkspaceId ? "#4a9" : "#444",
                  }}
                >
                  {w.id === activeWorkspaceId && (
                    <span style={{ color: "#4a9", fontWeight: "bold", fontSize: 11 }}>● ACTIVE</span>
                  )}
                  <span style={{ minWidth: 140 }}>{w.name}</span>
                  <code style={{ fontSize: 11, opacity: 0.6 }}>{w.id}</code>
                  <input
                    placeholder="new name"
                    value={editValues[w.id] ?? ""}
                    onChange={(e) => setEdit(w.id, e.target.value)}
                    style={{ width: 160 }}
                  />
                  <button onClick={() => handleUpdateWorkspace(w.id)} disabled={!editValues[w.id]}>Update</button>
                  {w.id !== activeWorkspaceId && (
                    <button onClick={() => handleSwitchWorkspace(w.id)}>Switch</button>
                  )}
                </div>
              ))}
            </div>
          </>
        )}
      </div>

      {/* ── SPACES ── */}
      {connected && workspaceRootStore && (
        <div style={section}>
          <h3>Spaces ({workspaceRootStore.spaceStore.all.length})</h3>

          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <input
              placeholder="Space name"
              value={spaceName}
              onChange={(e) => setSpaceName(e.target.value)}
              style={{ width: 240 }}
            />
            <button onClick={handleCreateSpace} disabled={!spaceName}>Create Space</button>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {workspaceRootStore.spaceStore.all.map((s) => (
              <div key={s.id} style={row}>
                <span style={{ minWidth: 140 }}>{s.name}</span>
                <code style={{ fontSize: 11, opacity: 0.6 }}>{s.id}</code>
                <input
                  placeholder="new name"
                  value={editValues[s.id] ?? ""}
                  onChange={(e) => setEdit(s.id, e.target.value)}
                  style={{ width: 160 }}
                />
                <button onClick={() => handleUpdateSpace(s.id)} disabled={!editValues[s.id]}>Update</button>
                <button onClick={() => handleDeleteSpace(s.id)}>Delete</button>
              </div>
            ))}
          </div>

          <div style={{ marginTop: 8, fontSize: 11, opacity: 0.5 }}>
            Statuses ({workspaceRootStore.statusStore.all.length}):&nbsp;
            {workspaceRootStore.statusStore.all.map((st) => `${st.name}(${st.category})`).join(", ")}
          </div>
        </div>
      )}

      {/* ── FOLDERS ── */}
      {connected && workspaceRootStore && (
        <div style={section}>
          <h3>Folders ({workspaceRootStore.folderStore.all.length})</h3>

          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <input
              placeholder="Folder name"
              value={folderName}
              onChange={(e) => setFolderName(e.target.value)}
              style={{ width: 200 }}
            />
            <select value={folderSpaceId} onChange={(e) => setFolderSpaceId(e.target.value)} style={{ width: 260 }}>
              <option value="">— pick a space —</option>
              {workspaceRootStore.spaceStore.all.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.id.slice(0, 8)})</option>
              ))}
            </select>
            <button onClick={handleCreateFolder} disabled={!folderName || !folderSpaceId}>Create Folder</button>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {workspaceRootStore.folderStore.all.map((f) => (
              <div key={f.id} style={row}>
                <span style={{ minWidth: 140 }}>{f.name}</span>
                <code style={{ fontSize: 11, opacity: 0.6 }}>{f.id}</code>
                <span style={{ fontSize: 11, opacity: 0.5 }}>space: {f.spaceId}</span>
                <input
                  placeholder="new name"
                  value={editValues[f.id] ?? ""}
                  onChange={(e) => setEdit(f.id, e.target.value)}
                  style={{ width: 160 }}
                />
                <button onClick={() => handleUpdateFolder(f.id)} disabled={!editValues[f.id]}>Update</button>
                <button onClick={() => handleDeleteFolder(f.id)}>Delete</button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TASKS ── */}
      {connected && workspaceRootStore && (
        <div style={section}>
          <h3>Tasks ({workspaceRootStore.taskStore.all.length})</h3>

          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <input
              placeholder="Task name"
              value={taskName}
              onChange={(e) => setTaskName(e.target.value)}
              style={{ width: 200 }}
            />
            <select value={taskSpaceId} onChange={(e) => { setTaskSpaceId(e.target.value); setTaskFolderId(""); }} style={{ width: 200 }}>
              <option value="">— space —</option>
              {workspaceRootStore.spaceStore.all.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.id.slice(0, 8)})</option>
              ))}
            </select>
            <select value={taskFolderId} onChange={(e) => setTaskFolderId(e.target.value)} style={{ width: 200 }}>
              <option value="">— folder (optional) —</option>
              {workspaceRootStore.folderStore.all
                .filter((f) => !taskSpaceId || f.spaceId === taskSpaceId)
                .map((f) => (
                  <option key={f.id} value={f.id}>{f.name} ({f.id.slice(0, 8)})</option>
                ))}
            </select>
            <button onClick={handleCreateTask} disabled={!taskName || !taskSpaceId}>Create Task</button>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {workspaceRootStore.taskStore.all.map((t) => (
              <div key={t.id} style={row}>
                <span style={{ minWidth: 140 }}>{t.name}</span>
                <code style={{ fontSize: 11, opacity: 0.6 }}>{t.id}</code>
                <span style={{ fontSize: 11, opacity: 0.5 }}>space: {t.spaceId}</span>
                <input
                  placeholder="new name"
                  value={editValues[t.id] ?? ""}
                  onChange={(e) => setEdit(t.id, e.target.value)}
                  style={{ width: 160 }}
                />
                <button onClick={() => handleUpdateTask(t.id)} disabled={!editValues[t.id]}>Update</button>
                <button onClick={() => handleDeleteTask(t.id)}>Delete</button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── LOG ── */}
      <div style={section}>
        <h3>Log</h3>
        <pre style={{ background: "#111", color: "#0f0", padding: 12, height: 300, overflow: "auto", fontSize: 12 }}>
          {log.join("\n")}
        </pre>
      </div>
    </div>
  );
});

export const Route = createFileRoute("/dev/sync-test")({
  component: SyncTestPage,
});
