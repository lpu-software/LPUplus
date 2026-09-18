import { useState, useRef, useEffect } from 'react';
import { SignalingClient } from '../signaling/SignalingClient';
import './AIAssistant.css';

interface Message {
  role: 'user' | 'assistant';
  content: string;
}

interface AIAssistantProps {
  signaling: SignalingClient;
  videoRef: React.RefObject<HTMLImageElement>;
}

export function AIAssistant({ signaling, videoRef }: AIAssistantProps) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  useEffect(() => {
    const handleAIResponse = (res: any) => {
      setLoading(false);
      if (res.success) {
        setMessages(prev => [...prev, { role: 'assistant', content: res.response }]);
      } else {
        setMessages(prev => [...prev, { role: 'assistant', content: `Error: ${res.error}` }]);
      }
    };

    signaling.on('ai_response', handleAIResponse);
    return () => signaling.off('ai_response', handleAIResponse);
  }, [signaling]);

  const captureFrame = (): string | null => {
    if (!videoRef.current || videoRef.current.naturalWidth === 0 || videoRef.current.naturalHeight === 0) return null;
    const canvas = document.createElement('canvas');
    canvas.width = videoRef.current.naturalWidth;
    canvas.height = videoRef.current.naturalHeight;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.drawImage(videoRef.current, 0, 0, canvas.width, canvas.height);
      return canvas.toDataURL('image/jpeg', 0.5); // compress it
    }
    return null;
  };

  const handleSend = () => {
    if (!input.trim() || loading) return;

    const userMessage = input.trim();
    setMessages(prev => [...prev, { role: 'user', content: userMessage }]);
    setInput('');
    setLoading(true);

    const frameBase64 = captureFrame();

    signaling.send({
      type: 'ai_request',
      requestId: Math.random().toString(36).substring(7),
      requestType: frameBase64 ? 'ScreenAnalysis' : 'Chat',
      prompt: userMessage,
      imageBase64: frameBase64
    });
  };

  return (
    <div className="ai-assistant-container">
      <div className="ai-header">
        <div className="ai-header-icon">
          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="white" strokeWidth="2.5">
            <path d="M12 2a5 5 0 0 1 5 5v1h1a3 3 0 0 1 3 3v5a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3v-5a3 3 0 0 1 3-3h1V7a5 5 0 0 1 5-5z"/>
          </svg>
        </div>
        <h3>AI Assistant</h3>
      </div>
      
      <div className="ai-messages">
        {messages.length === 0 && (
          <div className="ai-empty">
            <p>Ask me to analyze the current screen or help with remote diagnostics.</p>
          </div>
        )}
        {messages.map((msg, idx) => (
          <div key={idx} className={`ai-message ${msg.role}`}>
            <div className="ai-message-bubble">{msg.content}</div>
          </div>
        ))}
        {loading && (
          <div className="ai-message assistant">
            <div className="ai-message-bubble loading-dots">Thinking...</div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      <div className="ai-input-area">
        <input 
          type="text" 
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleSend()}
          placeholder="Ask a question..."
          disabled={loading}
        />
        <button onClick={handleSend} disabled={!input.trim() || loading}>
          Send
        </button>
      </div>
    </div>
  );
}
