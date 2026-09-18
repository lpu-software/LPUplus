import { useState, useEffect, useRef } from 'react';
import { SignalingClient } from './signaling/SignalingClient';
import type { PairResponseMessage } from './signaling/SignalingClient';
import { WebRtcClient } from './webrtc/WebRtcClient';
import { AIAssistant } from './components/AIAssistant';
import './App.css';

function App() {
  const [pairingCode, setPairingCode] = useState('');
  const [status, setStatus] = useState<'idle' | 'connecting' | 'waiting_approval' | 'connected' | 'error'>('idle');
  const [errorMessage, setErrorMessage] = useState('');
  const [deviceName, setDeviceName] = useState('');
  const [streamActive, setStreamActive] = useState(false);
  const [isAIOpen, setIsAIOpen] = useState(false);
  
  const signaling = useRef<SignalingClient | null>(null);
  const webrtc = useRef<WebRtcClient | null>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const sessionRef = useRef<HTMLDivElement>(null);

  // Expose imgRef globally for AIAssistant
  useEffect(() => {
    (window as any).remoteVideoRef = imgRef;
  }, []);

  const [toolbarPos, setToolbarPos] = useState({ x: 20, y: 20 });
  const [isDragging, setIsDragging] = useState(false);
  const dragRef = useRef({ startX: 0, startY: 0, initialX: 0, initialY: 0 });

  const startDrag = (e: React.MouseEvent) => {
    setIsDragging(true);
    dragRef.current = {
      startX: e.clientX,
      startY: e.clientY,
      initialX: toolbarPos.x,
      initialY: toolbarPos.y
    };
  };

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDragging) return;
      const dx = e.clientX - dragRef.current.startX;
      const dy = e.clientY - dragRef.current.startY;
      setToolbarPos({
        x: dragRef.current.initialX + dx,
        y: dragRef.current.initialY + dy
      });
    };
    const handleMouseUp = () => setIsDragging(false);

    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
    }
    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging]);




  const toggleFullscreen = async () => {
    if (!document.fullscreenElement) {
      await sessionRef.current?.requestFullscreen();
    } else {
      await document.exitFullscreen();
    }
  };

  const connect = async () => {
    if (!pairingCode || pairingCode.length < 9) {
      setErrorMessage("Please enter a valid pairing code (e.g. XXXX-XXXX)");
      return;
    }
    try {
      setStatus('connecting');
      setErrorMessage('');
      signaling.current = new SignalingClient();
      await signaling.current.connect();

      signaling.current.on('pair_response', async (res: PairResponseMessage) => {
        if (!res.success) {
          setStatus('error');
          setErrorMessage(res.error || "Pairing failed.");
          signaling.current?.disconnect();
          return;
        }
        if (res.sessionToken) {
          setStatus('connected');
          setDeviceName(res.deviceName || 'Remote Device');
          webrtc.current = new WebRtcClient(signaling.current!);

          webrtc.current.onConnected = () => {
            if (imgRef.current) {
              setStreamActive(true);
            }
          };
          let isDecoding = false;
          let nextFrameBlob: Blob | null = null;

          const processNextFrame = () => {
            if (isDecoding || !nextFrameBlob || !imgRef.current) return;
            
            isDecoding = true;
            const url = URL.createObjectURL(nextFrameBlob);
            nextFrameBlob = null; // Consume the frame
            
            const prevUrl = imgRef.current.src;
            
            imgRef.current.onload = () => {
              if (prevUrl && prevUrl.startsWith('blob:')) {
                URL.revokeObjectURL(prevUrl);
              }
              isDecoding = false;
              // Instantly process the next frame if one arrived while we were decoding
              processNextFrame(); 
            };
            
            imgRef.current.onerror = () => {
              isDecoding = false;
              processNextFrame();
            };
            
            imgRef.current.src = url;
          };

          webrtc.current.onFrame = (frameBuffer) => {
            nextFrameBlob = new Blob([frameBuffer], { type: 'image/jpeg' });
            processNextFrame();
          };
          webrtc.current.onDisconnected = () => {
            setStatus('error');
            setErrorMessage("Connection lost.");
          };
          await webrtc.current.initiateConnection();
        } else {
          setStatus('waiting_approval');
        }
      });

      signaling.current.send({ type: 'pair_request', pairingCode });
    } catch {
      setStatus('error');
      setErrorMessage("Failed to connect to signaling server.");
    }
  };

  // Keyboard forwarding
  useEffect(() => {
    if (status !== 'connected') return;
    const down = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
      e.preventDefault();
      webrtc.current?.sendControlMessage({ type: 'key_down', key: e.key });
    };
    const up = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
      e.preventDefault();
      webrtc.current?.sendControlMessage({ type: 'key_up', key: e.key });
    };
    window.addEventListener('keydown', down);
    window.addEventListener('keyup', up);
    return () => { window.removeEventListener('keydown', down); window.removeEventListener('keyup', up); };
  }, [status]);

  // Mouse helpers
  const getCoords = (e: React.MouseEvent<HTMLImageElement>) => {
    const img = e.currentTarget;
    const rect = img.getBoundingClientRect();
    
    // Calculate the actual rendered dimensions of the image (object-fit: contain)
    const imgAspect = img.naturalWidth / img.naturalHeight;
    const boxAspect = rect.width / rect.height;
    
    let renderWidth, renderHeight, offsetX, offsetY;
    if (boxAspect > imgAspect) {
      // Box is wider than image (pillarboxes on left/right)
      renderHeight = rect.height;
      renderWidth = rect.height * imgAspect;
      offsetX = (rect.width - renderWidth) / 2;
      offsetY = 0;
    } else {
      // Box is taller than image (letterboxes on top/bottom)
      renderWidth = rect.width;
      renderHeight = rect.width / imgAspect;
      offsetX = 0;
      offsetY = (rect.height - renderHeight) / 2;
    }
    
    // Mouse coords relative to the rendered image
    const mouseX = e.clientX - rect.left - offsetX;
    const mouseY = e.clientY - rect.top - offsetY;
    
    // Clamp to image bounds
    if (mouseX < 0 || mouseX > renderWidth || mouseY < 0 || mouseY > renderHeight) {
      return null; // out of bounds (clicked on black bar)
    }

    return {
      x: mouseX / renderWidth,
      y: mouseY / renderHeight
    };
  };
  const buttonName = (b: number) => b === 1 ? 'middle' : b === 2 ? 'right' : 'left';

  // ─── RENDER ───

  if (status !== 'connected') {
    return (
      <div className="login-page">
        <div className="login-card">
          <div className="login-logo">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#4f7cff" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="2" y="3" width="20" height="14" rx="2"/>
              <line x1="8" y1="21" x2="16" y2="21"/>
              <line x1="12" y1="17" x2="12" y2="21"/>
            </svg>
            <span className="login-brand">LPU+</span>
          </div>
          <h2 className="login-title">Remote Support</h2>
          <p className="login-subtitle">Enter the pairing code from the host machine</p>
          <input
            className="login-input"
            type="text"
            placeholder="XXXX-XXXX"
            value={pairingCode}
            onChange={(e) => setPairingCode(e.target.value.toUpperCase())}
            disabled={status === 'connecting' || status === 'waiting_approval'}
            maxLength={9}
            onKeyDown={(e) => e.key === 'Enter' && connect()}
          />
          {errorMessage && <div className="login-error">{errorMessage}</div>}
          {status === 'waiting_approval' && (
            <div className="login-waiting">
              <div className="login-spinner" />
              Waiting for host approval...
            </div>
          )}
          <button className="login-btn" onClick={connect} disabled={status === 'connecting' || status === 'waiting_approval'}>
            {status === 'connecting' ? 'Connecting...' : status === 'waiting_approval' ? 'Waiting...' : 'Connect'}
          </button>
        </div>
      </div>
    );
  }
  if (status === 'connected') {
    return (
      <div ref={sessionRef} className="session">
        
        {/* Floating Movable Toolbar */}
        <div 
          className="floating-toolbar" 
          style={{ top: toolbarPos.y, left: toolbarPos.x }}
        >
          <div className="drag-handle" onMouseDown={startDrag}>
            <span/><span/><span/>
          </div>
          <div className="topbar-left">
            <div className="topbar-dot" />
            <div className="topbar-name" title={deviceName}>{deviceName || 'Remote Device'}</div>
          </div>
          <div className="topbar-sep" />
          <div className="topbar-actions">
            <button className="topbar-btn" onClick={() => setIsAIOpen(true)} title="AI Assistant">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M12 2a10 10 0 1010 10H12V2z"/>
                <path d="M12 2a10 10 0 00-10 10h10V2z"/>
              </svg>
            </button>
            <button className="topbar-btn" onClick={toggleFullscreen} title="Fullscreen">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M8 3H5a2 2 0 00-2 2v3m18 0V5a2 2 0 00-2-2h-3m0 18h3a2 2 0 002-2v-3M3 16v3a2 2 0 002 2h3" />
              </svg>
            </button>
            <button className="topbar-btn topbar-end" onClick={() => window.location.reload()}>
              End
            </button>
          </div>
        </div>

        {/* Screen Area */}
        <div className="screen-area">
          <img
            ref={imgRef}
            className="screen-img"
            draggable={false}
            onContextMenu={(e) => e.preventDefault()}
            onMouseMove={(e) => {
              const c = getCoords(e);
              if (c) webrtc.current?.sendControlMessage({ type: 'mouse_move', x: c.x, y: c.y });
            }}
            onMouseDown={(e) => {
              const c = getCoords(e);
              if (c) webrtc.current?.sendControlMessage({ type: 'mouse_down', button: buttonName(e.button), x: c.x, y: c.y });
            }}
            onMouseUp={(e) => {
              const c = getCoords(e);
              if (c) webrtc.current?.sendControlMessage({ type: 'mouse_up', button: buttonName(e.button), x: c.x, y: c.y });
            }}
            onWheel={(e) => {
              webrtc.current?.sendControlMessage({ type: 'mouse_wheel', deltaX: e.deltaX, deltaY: e.deltaY });
            }}
          />
        {!streamActive && (
          <div className="screen-placeholder">
            <div className="login-spinner" />
            <p>Waiting for screen...</p>
          </div>
        )}
      </div>

      {/* AI FAB */}
      <button className={`ai-fab ${isAIOpen ? 'ai-fab-active' : ''}`} onClick={() => setIsAIOpen(!isAIOpen)} title="AI Assistant">
        {isAIOpen ? '✕' : '✦'}
      </button>

      {/* AI Drawer */}
      <div className={`ai-drawer ${isAIOpen ? 'ai-drawer-open' : ''}`}>
        {signaling.current && <AIAssistant signaling={signaling.current} videoRef={imgRef as React.RefObject<HTMLImageElement>} />}
      </div>
    </div>
  );
  }
}
export default App;
