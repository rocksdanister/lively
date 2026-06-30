using Lively.Models.Enums;

namespace Lively.Models.Taskbar;

public sealed class TaskbarIpcRequest
{
    public string RequestId { get; set; }
    public TaskbarIpcCommand Type { get; set; }
    public TaskbarTheme Theme { get; set; } = TaskbarTheme.none;
    public uint Color { get; set; }
    public float BlurRadius { get; set; }
}
