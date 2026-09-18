export interface PairResponseMessage {
  type: "pair_response";
  success: boolean;
  sessionToken?: string;
  deviceName?: string;
  deviceId?: string;
  error?: string;
}

export interface SdpOfferMessage {
  type: "sdp_offer";
  sdp: string;
}

export interface SdpAnswerMessage {
  type: "sdp_answer";
  sdp: string;
}

export interface IceCandidateMessage {
  type: "ice_candidate";
  candidate: string;
  sdpMid: string | null;
  sdpMLineIndex: number | null;
}

type SignalingCallback = (msg: any) => void;

export class SignalingClient {
  private ws: WebSocket | null = null;
  private url: string;
  private listeners: Record<string, SignalingCallback[]> = {};

  constructor(url: string = "") {
    this.url = url;
  }

  public async connect(): Promise<void> {
    if (this.ws?.readyState === WebSocket.OPEN) return;

    // Use environment variable, fallback to current host if deploying together, or default to localhost:5121 for local dev
    const wsUrl = this.url || import.meta.env.VITE_WS_URL || 
                  (window.location.hostname === 'localhost' ? 'ws://localhost:5121' : `wss://${window.location.host}`);

    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(wsUrl);
      
      this.ws.onopen = () => resolve();
      
      this.ws.onerror = (err) => reject(err);
      
      this.ws.onmessage = (event) => {
        try {
          const envelope = JSON.parse(event.data);
          // C# server sends { type: "...", payload: { ... } }
          if (envelope.type && envelope.payload) {
             this.emit(envelope.type, envelope.payload);
          }
        } catch (e) {
          console.error("Failed to parse signaling message", e);
        }
      };
      
      this.ws.onclose = () => {
        this.emit("close", null);
      };
    });
  }

  public send(msg: any) {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      const envelope = {
        v: 1,
        type: msg.type,
        ts: Date.now(),
        payload: msg
      };
      this.ws.send(JSON.stringify(envelope));
    }
  }

  public on(type: string, callback: SignalingCallback) {
    if (!this.listeners[type]) {
      this.listeners[type] = [];
    }
    this.listeners[type].push(callback);
  }

  public off(type: string, callback: SignalingCallback) {
    if (this.listeners[type]) {
      this.listeners[type] = this.listeners[type].filter(cb => cb !== callback);
    }
  }

  private emit(type: string, data: any) {
    const typeListeners = this.listeners[type];
    if (typeListeners) {
      typeListeners.forEach(cb => cb(data));
    }
  }

  public disconnect() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }
}
