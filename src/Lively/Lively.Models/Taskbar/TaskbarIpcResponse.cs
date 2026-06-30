namespace Lively.Models.Taskbar;

public sealed class TaskbarIpcResponse
{
    public string RequestId { get; set; }
    public bool Success { get; set; }
    public TaskbarIpcStatus Status { get; set; }
    public string Error { get; set; }
    public int HResult { get; set; }
}
