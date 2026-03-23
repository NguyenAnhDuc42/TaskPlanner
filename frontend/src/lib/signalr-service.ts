import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

class SignalRService {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private url: string = "https://localhost:7285/hubs/workspace";

  public async startConnection(): Promise<void> {
    // If already connected, return
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    // If already starting, return the existing promise
    if (this.startPromise) {
      return this.startPromise;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.url, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.startPromise = (async () => {
      try {
        await this.connection!.start();
        console.log("[SignalR] Connection started");
      } catch (err) {
        console.error("[SignalR] Error while starting connection: " + err);
        this.startPromise = null;
        setTimeout(() => this.startConnection(), 5000);
        throw err;
      } finally {
        this.startPromise = null;
      }
    })();

    return this.startPromise;
  }

  public on(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.connection) {
      console.warn(
        "[SignalR] Cannot register listener, connection not initialized",
      );
      return;
    }
    this.connection.on(eventName, callback);
  }

  public off(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.connection) return;
    this.connection.off(eventName, callback);
  }

  public async invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
    if (!this.connection) {
      throw new Error("[SignalR] Connection not initialized");
    }
    return this.connection.invoke(methodName, ...args);
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();
