import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

class SignalRService {
  private connection: HubConnection | null = null;
  private url: string = "https://localhost:7285/hubs/workspace";

  public async startConnection(): Promise<void> {
    if (
      this.connection &&
      this.connection.state !== HubConnectionState.Disconnected
    ) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.url, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    try {
      await this.connection.start();
      console.log("[SignalR] Connection started");
    } catch (err) {
      console.error("[SignalR] Error while starting connection: " + err);
      setTimeout(() => this.startConnection(), 5000);
    }
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

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();
