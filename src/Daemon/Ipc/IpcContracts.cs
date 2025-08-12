namespace BrandshareDamSync.Daemon.Ipc;

public enum IpcCommand
{
    Ping,
    GetStatus,
    Start,
    Stop
}

public sealed record IpcRequest(IpcCommand Command, string? Argument = null);

public sealed record IpcResponse(bool Ok, string Message, string? PayloadJson = null);