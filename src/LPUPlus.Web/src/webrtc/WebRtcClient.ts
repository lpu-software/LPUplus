import { SignalingClient } from "../signaling/SignalingClient";
import type { SdpOfferMessage, IceCandidateMessage, SdpAnswerMessage } from "../signaling/SignalingClient";

export class WebRtcClient {
  private pc: RTCPeerConnection;
  private signaling: SignalingClient;
  private controlChannel: RTCDataChannel | null = null;
  private videoChannel: RTCDataChannel | null = null;
  private iceQueue: RTCIceCandidateInit[] = [];

  public onConnected?: () => void;
  public onDisconnected?: () => void;
  public onFrame?: (frameData: ArrayBuffer) => void;

  constructor(signaling: SignalingClient) {
    this.signaling = signaling;
    
    this.pc = new RTCPeerConnection({
      iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
    });

    // Create the video stream channel
    this.videoChannel = this.pc.createDataChannel("video_stream");
    this.videoChannel.binaryType = "arraybuffer";
    this.videoChannel.onmessage = (event) => {
      if (this.onFrame) {
        this.onFrame(event.data);
      }
    };

    // Handle ICE candidates
    this.pc.onicecandidate = (event) => {
      if (event.candidate) {
        this.signaling.send({
          type: "ice_candidate",
          candidate: event.candidate.candidate,
          sdpMid: event.candidate.sdpMid,
          sdpMLineIndex: event.candidate.sdpMLineIndex
        } as IceCandidateMessage);
      }
    };

    // Handle connection state
    this.pc.onconnectionstatechange = () => {
      console.log("WebRTC connection state:", this.pc.connectionState);
      if (this.pc.connectionState === "connected") {
        this.onConnected?.();
      } else if (this.pc.connectionState === "disconnected" || this.pc.connectionState === "failed") {
        this.onDisconnected?.();
      }
    };

    // Listen for signaling events
    this.signaling.on("sdp_answer", async (msg: SdpAnswerMessage) => {
      console.log("Received SDP Answer");
      await this.pc.setRemoteDescription({ type: "answer", sdp: msg.sdp });
      
      // Process any queued candidates
      while (this.iceQueue.length > 0) {
        const candidate = this.iceQueue.shift();
        if (candidate) {
          try {
            await this.pc.addIceCandidate(candidate);
          } catch (e) {
            console.warn("Failed to add queued ICE candidate:", e);
          }
        }
      }
    });

    this.signaling.on("ice_candidate", async (msg: IceCandidateMessage) => {
      const candidateInit = {
        candidate: msg.candidate,
        sdpMid: msg.sdpMid,
        sdpMLineIndex: msg.sdpMLineIndex ?? 0
      };
      
      if (this.pc.remoteDescription) {
        try {
          await this.pc.addIceCandidate(candidateInit);
        } catch (e) {
          console.warn("Failed to add ICE candidate:", e);
        }
      } else {
        this.iceQueue.push(candidateInit);
      }
    });
  }

  public async initiateConnection() {
    // Create the control DataChannel (for mouse/keyboard)
    // Video is streamed via HTTP MJPEG — no DataChannel needed for video
    this.controlChannel = this.pc.createDataChannel("control");
    this.controlChannel.onopen = () => {
      console.log("✅ Control channel opened");
    };
    this.controlChannel.onmessage = (e) => {
      console.log("Control msg:", e.data);
    };

    // Create SDP offer — DataChannel only (no video/audio tracks needed)
    const offer = await this.pc.createOffer();
    await this.pc.setLocalDescription(offer);

    this.signaling.send({
      type: "sdp_offer",
      sdp: offer.sdp
    } as SdpOfferMessage);
  }

  public sendControlMessage(message: any) {
    if (this.controlChannel && this.controlChannel.readyState === 'open') {
      const envelope = {
        v: 1,
        type: message.type,
        ts: Date.now(),
        payload: message
      };
      this.controlChannel.send(JSON.stringify(envelope));
    } else {
      // Don't spam the console — silently skip
    }
  }

  public disconnect() {
    this.pc.close();
  }
}
