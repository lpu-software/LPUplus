using SIPSorcery.Net;
using LPUPlus.Protocol.Messages;
using LPUPlus.Agent.Signaling;
using SIPSorceryMedia.FFmpeg;
using SIPSorcery.Media;
using System.Linq;
using System.Runtime.InteropServices;
using LPUPlus.Agent.Input;
using LPUPlus.Protocol.Serialization;

namespace LPUPlus.Agent.WebRTC;

/// <summary>
/// Manages the WebRTC RTCPeerConnection for the Host Agent.
/// Video is streamed via a local HTTP MJPEG server (not DataChannel).
/// WebRTC is used for the control channel (mouse/keyboard).
/// </summary>
public sealed class WebRtcManager : IDisposable
{
    private readonly SignalingClient _signaling;
    private RTCPeerConnection? _peerConnection;
    private RTCDataChannel? _controlChannel;
    private RTCDataChannel? _videoChannel;
    private readonly IInputInjector _inputInjector;

    private MjpegScreenSource? _mjpegSource;
    


    public event Action<string>? OnControlMessageReceived;

    public WebRtcManager(SignalingClient signaling)
    {
        _signaling = signaling;
        _signaling.OnSdpOffer += HandleSdpOfferAsync;
        _signaling.OnIceCandidate += HandleIceCandidateAsync;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _inputInjector = new MacInputInjector();
        }
        else
        {
            _inputInjector = new WindowsInputInjector();
        }
    }

    private async Task HandleSdpOfferAsync(SdpOfferMessage offer)
    {
        try
        {
            Console.WriteLine($"[WebRTC] Received SDP Offer");

            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer>
                {
                    new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
                }
            };

            FFmpegInit.Initialise(null, "/opt/homebrew/lib");
            
            _peerConnection = new RTCPeerConnection(config);

            // Start the screen capture source
            _mjpegSource = new MjpegScreenSource();
            _mjpegSource.OnJpegFrame += (jpegBytes) =>
            {
                if (_videoChannel != null && _videoChannel.readyState == RTCDataChannelState.open)
                {
                    if (_videoChannel.bufferedAmount > 0)
                    {
                        Console.WriteLine($"[WebRTC] Dropped frame (Network congested: {_videoChannel.bufferedAmount} bytes buffered)");
                        return;
                    }

                    if (jpegBytes.Length <= 262144)
                    {
                        try { _videoChannel.send(jpegBytes); } catch { }
                    }
                    else
                    {
                        Console.WriteLine($"[WebRTC] Dropped frame (too large: {jpegBytes.Length} bytes)");
                    }
                }
            };

            // Data Channels are created by the caller (frontend). We listen for them.
            _peerConnection.ondatachannel += (channel) =>
            {
                Console.WriteLine($"[WebRTC] DataChannel created: {channel.label}");
                if (channel.label == "control")
                {
                    _controlChannel = channel;
                    _controlChannel.onmessage += (dc, protocol, data) =>
                    {
                        if (protocol == SIPSorcery.Net.DataChannelPayloadProtocols.WebRTC_String && data != null)
                        {
                            var text = System.Text.Encoding.UTF8.GetString(data);
                            Task.Run(() => 
                            {
                                OnControlMessageReceived?.Invoke(text);
                                ProcessControlMessage(text);
                            });
                        }
                    };
                }
                else if (channel.label == "video_stream")
                {
                    _videoChannel = channel;
                }
            };

            // ICE Candidates
            _peerConnection.onicecandidate += async (candidate) =>
            {
                if (candidate != null)
                {
                    await _signaling.SendMessageAsync(new IceCandidateMessage
                    {
                        Candidate = candidate.candidate,
                        SdpMid = candidate.sdpMid,
                        SdpMLineIndex = candidate.sdpMLineIndex
                    });
                }
            };



            // Connection State
            _peerConnection.onconnectionstatechange += async (state) =>
            {
                Console.WriteLine($"[WebRTC] Connection state: {state}");
                if (state == RTCPeerConnectionState.connected)
                {
                    if (_mjpegSource != null)
                    {
                        _mjpegSource.Start();
                        Console.WriteLine("[WebRTC] MJPEG source started (1080p @ 30fps via HTTP).");
                    }
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    if (_mjpegSource != null)
                    {
                        _mjpegSource.Stop();
                        Console.WriteLine("[WebRTC] MJPEG source stopped.");
                    }
                }
            };

            // Process Offer
            var result = _peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
            {
                type = RTCSdpType.offer,
                sdp = offer.Sdp
            });

            if (result != SIPSorcery.Net.SetDescriptionResultEnum.OK)
            {
                Console.WriteLine($"[WebRTC] Error setting remote description: {result}");
                return;
            }

            // Create Answer
            var answer = _peerConnection.createAnswer(null);
            await _peerConnection.setLocalDescription(answer);

            // Send Answer
            await _signaling.SendMessageAsync(new SdpAnswerMessage
            {
                Sdp = answer.sdp
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION IN SDP OFFER: " + ex);
            throw;
        }
    }

    private Task HandleIceCandidateAsync(IceCandidateMessage ice)
    {
        if (_peerConnection != null)
        {
            var candidateInit = new RTCIceCandidateInit
            {
                candidate = ice.Candidate,
                sdpMid = ice.SdpMid,
                sdpMLineIndex = (ushort)(ice.SdpMLineIndex ?? 0)
            };
            _peerConnection.addIceCandidate(candidateInit);
        }
        return Task.CompletedTask;
    }

    private void ProcessControlMessage(string json)
    {
        try
        {
            Console.WriteLine($"[WebRTC] Control Message: {json}");
            var msg = MessageSerializer.Deserialize(json);
            if (msg == null) return;

            switch (msg)
            {
                case MouseMoveMessage mm:
                    _inputInjector.InjectMouseMove(mm.X, mm.Y);
                    break;
                case MouseDownMessage md:
                    _inputInjector.InjectMouseDown(md.Button, md.X, md.Y);
                    break;
                case MouseUpMessage mu:
                    _inputInjector.InjectMouseUp(mu.Button, mu.X, mu.Y);
                    break;
                case MouseWheelMessage mw:
                    _inputInjector.InjectMouseWheel(mw.DeltaX, mw.DeltaY);
                    break;
                case KeyDownMessage kd:
                    _inputInjector.InjectKeyDown(kd.Key);
                    break;
                case KeyUpMessage ku:
                    _inputInjector.InjectKeyUp(ku.Key);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRTC] Error processing control message: {ex}");
        }
    }

    public void Dispose()
    {
        _signaling.OnSdpOffer -= HandleSdpOfferAsync;
        _signaling.OnIceCandidate -= HandleIceCandidateAsync;

        _mjpegSource?.Stop();
        _mjpegSource?.Dispose();
        
        _peerConnection?.Close("disposing");
        _peerConnection?.Dispose();
    }
}
