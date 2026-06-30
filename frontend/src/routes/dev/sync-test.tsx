import { createFileRoute } from "@tanstack/react-router";
import { useState, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { RootStore } from "@/stores/root.store";
import { SyncEngine } from "@/sync/sync-engine";
import { TaskMutations } from "@/mutations/task.mutations";

const SyncTestPage = observer(function SyncTestPage() {
  const rootStore = useMemo(() => new RootStore(), []);
  const syncEngine = useMemo(() => new SyncEngine(rootStore), [rootStore]);
  const taskMutations = useMemo(() => new TaskMutations(rootStore, syncEngine), [rootStore, syncEngine]);

  const [workspaceId, setWorkspaceId] = useState("");
  const [spaceId, setSpaceId] = useState("");
  const [connected, setConnected] = useState(false);
  const [taskName, setTaskName] = useState("");
  const [log, setLog] = useState<string[]>([]);

  const addLog = (msg: string) => setLog((prev) => [`${new Date().toLocaleTimeString()} — ${msg}`, ...prev]);

  const handleConnect = async () => {
    if (!workspaceId) return;
    try {
      addLog(`Switching to workspace ${workspaceId}...`);
      await rootStore.switchWorkspace(workspaceId);
      addLog("IndexedDB hydrated. Connecting sync engine...");
      await syncEngine.init(workspaceId);
      setConnected(true);
      addLog("Connected. lastSyncId in metadata: " + (await rootStore.metadataDB?.getLastSyncId()));
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleCreateTask = async () => {
    if (!taskName) return;
    try {
      const wasOnline = rootStore.isOnline;
      addLog(`Creating task "${taskName}" (isOnline=${wasOnline})...`);
      const record = await taskMutations.create({ name: taskName, spaceId });
      addLog(
        wasOnline
          ? `Task created locally with id ${record.id}. Waiting for Delta confirmation...`
          : `Task created locally with id ${record.id}. Queued in __transactions — will send when back online.`
      );
      setTaskName("");
    } catch (err) {
      addLog("ERROR: " + (err instanceof Error ? err.message : String(err)));
    }
  };

  const handleFlushQueue = async () => {
    addLog("Flushing pending transaction queue...");
    await syncEngine.flushQueue();
    addLog("Flush complete.");
  };

  return (
    <div style={{ padding: 24, fontFamily: "monospace", maxWidth: 800 }}>
      <h2>Sync Engine Test Page</h2>

      <div style={{ marginBottom: 16 }}>
        <input
          placeholder="Workspace ID"
          value={workspaceId}
          onChange={(e) => setWorkspaceId(e.target.value)}
          style={{ width: 300, marginRight: 8 }}
          disabled={connected}
        />
        <button onClick={handleConnect} disabled={connected || !workspaceId}>
          {connected ? "Connected" : "Connect"}
        </button>
      </div>

      {connected && (
        <div style={{ marginBottom: 16 }}>
          <input
            placeholder="Space ID (required — tasks must belong to a space)"
            value={spaceId}
            onChange={(e) => setSpaceId(e.target.value)}
            style={{ width: 340, marginRight: 8 }}
          />
          <br />
          <input
            placeholder="Task name"
            value={taskName}
            onChange={(e) => setTaskName(e.target.value)}
            style={{ width: 300, marginRight: 8, marginTop: 8 }}
          />
          <button onClick={handleCreateTask} disabled={!taskName || !spaceId}>
            Create Task
          </button>
          <button
            onClick={() => {
              const next = !rootStore.isOnline;
              rootStore.setOnline(next);
              addLog(`isOnline manually set to ${next}`);
            }}
            style={{ marginLeft: 16 }}
          >
            {rootStore.isOnline ? "Go Offline" : "Go Online"}
          </button>
          <span style={{ marginLeft: 8, color: rootStore.isOnline ? "lightgreen" : "salmon" }}>
            {rootStore.isOnline ? "● online" : "● offline"}
          </span>
          <button onClick={handleFlushQueue} style={{ marginLeft: 16 }}>
            Flush Queue
          </button>
        </div>
      )}

      <h3>Tasks in store ({rootStore.taskStore.all.length})</h3>
      <ul>
        {rootStore.taskStore.all.map((t) => (
          <li key={t.id}>
            {t.name} — <code>{t.id}</code>
          </li>
        ))}
      </ul>

      <h3>Log</h3>
      <pre style={{ background: "#111", color: "#0f0", padding: 12, height: 300, overflow: "auto" }}>
        {log.join("\n")}
      </pre>
    </div>
  );
});

export const Route = createFileRoute("/dev/sync-test")({
  component: SyncTestPage,
});
