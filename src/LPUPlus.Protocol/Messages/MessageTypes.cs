namespace LPUPlus.Protocol.Messages;

/// <summary>
/// All protocol message type constants. Versioned and centralized.
/// </summary>
public static class MessageTypes
{
    // ── Protocol version ──
    public const int ProtocolVersion = 1;

    // ── Auth ──
    public const string HostRegister = "host_register";
    public const string PairRequest = "pair_request";
    public const string PairResponse = "pair_response";

    // ── Session ──
    public const string SessionRequest = "session_request";
    public const string SessionApproved = "session_approved";
    public const string SessionRejected = "session_rejected";
    public const string SessionEnded = "session_ended";
    public const string PermissionUpdate = "permission_update";

    // ── WebRTC Signaling ──
    public const string SdpOffer = "sdp_offer";
    public const string SdpAnswer = "sdp_answer";
    public const string IceCandidate = "ice_candidate";

    // ── Cursor ──
    public const string CursorUpdate = "cursor_update";

    // ── Input ──
    public const string MouseMove = "mouse_move";
    public const string MouseDown = "mouse_down";
    public const string MouseUp = "mouse_up";
    public const string MouseWheel = "mouse_wheel";
    public const string KeyDown = "key_down";
    public const string KeyUp = "key_up";

    // ── Display ──
    public const string MonitorList = "monitor_list";
    public const string MonitorSwitch = "monitor_switch";
    public const string DisplayChanged = "display_changed";

    // ── Files ──
    public const string FileListRequest = "file_list_request";
    public const string FileListResponse = "file_list_response";
    public const string FileDownloadRequest = "file_download_request";
    public const string FileDownloadStart = "file_download_start";
    public const string FileUploadStart = "file_upload_start";
    public const string FileUploadAck = "file_upload_ack";
    public const string FileChunk = "file_chunk";
    public const string FileChunkAck = "file_chunk_ack";
    public const string FileTransferComplete = "file_transfer_complete";
    public const string FileDelete = "file_delete";
    public const string FileRename = "file_rename";
    public const string FileCreateFolder = "file_create_folder";
    public const string FileOperationResult = "file_operation_result";

    // ── Terminal ──
    public const string TerminalStart = "terminal_start";
    public const string TerminalStarted = "terminal_started";
    public const string TerminalInput = "terminal_input";
    public const string TerminalOutput = "terminal_output";
    public const string TerminalResize = "terminal_resize";
    public const string TerminalExit = "terminal_exit";
    public const string TerminalError = "terminal_error";

    // ── Performance ──
    public const string PerformanceUpdate = "performance_update";

    // ── AI ──
    public const string AIRequest = "ai_request";
    public const string AIResponse = "ai_response";
    public const string ResearchRequest = "research_request";
    public const string ResearchResponse = "research_response";

    // ── Error ──
    public const string Error = "error";
}
