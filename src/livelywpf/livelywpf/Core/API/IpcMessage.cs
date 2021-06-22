using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Core.API
{
    public enum MessageType
    {
        msg_hwnd,
        msg_console,
        msg_wploaded,
        msg_screenshot,
        cmd_reload,
        cmd_close,
        cmd_screenshot,
        lsp_perfcntr,
        lsp_nowplaying,
        lp_slider,
        lp_textbox,
        lp_dropdown,
        lp_fdropdown,
        lp_button,
        lp_cpicker,
        lp_chekbox,
    }

    public enum ConsoleMessageType
    {
        log,
        error,
        console
    }

    public enum ScreenshotFormat
    {
        jpeg,
        png,
        webp,
        bmp
    }

    [Serializable]
    public abstract class IpcMessage
    {
        [JsonProperty(Order = -2)]
        public MessageType Type { get; }
        public IpcMessage(MessageType type)
        {
            this.Type = type;
        }
    }

    [Serializable]
    public class LivelyMessageConsole : IpcMessage
    {
        public string Message { get; set; }
        public ConsoleMessageType Category { get; set; }
        public LivelyMessageConsole() : base(MessageType.msg_console)
        {
        }
    }

    [Serializable]
    public class LivelyMessageHwnd : IpcMessage
    {
        public long Hwnd { get; set; }
        public LivelyMessageHwnd() : base(MessageType.msg_hwnd)
        {
        }
    }

    [Serializable]
    public class LivelyMessageScreenshot : IpcMessage
    {
        public string FileName { get; set; }
        public bool Success { get; set; }
        public LivelyMessageScreenshot() : base(MessageType.msg_screenshot)
        {
        }
    }

    [Serializable]
    public class LivelyMessageWallpaperLoaded : IpcMessage
    {
        public bool Success { get; set; }
        public LivelyMessageWallpaperLoaded() : base(MessageType.msg_wploaded)
        {
        }
    }

    [Serializable]
    public class LivelyCloseCmd : IpcMessage
    {
        public LivelyCloseCmd() : base(MessageType.cmd_close)
        {
        }
    }

    [Serializable]
    public class LivelyReloadCmd : IpcMessage
    {
        public LivelyReloadCmd() : base(MessageType.cmd_reload)
        {
        }
    }

    [Serializable]
    public class LivelyScreenshotCmd : IpcMessage
    {
        public ScreenshotFormat Format { get; set; }
        public string FilePath { get; set; }
        public uint Delay { get; set; }
        public LivelyScreenshotCmd() : base(MessageType.cmd_screenshot)
        {
        }
    }

    [Serializable]
    public class LivelySystemInformation : IpcMessage
    {
        public Helpers.HWUsageMonitorEventArgs Info { get; set; }
        public LivelySystemInformation() : base(MessageType.cmd_reload)
        {
        }
    }

    [Serializable]
    public class LivelySystemNowPlaying : IpcMessage
    {
        public Helpers.NowPlayingEventArgs Info { get; set; }
        public LivelySystemNowPlaying() : base(MessageType.cmd_reload)
        {
        }
    }

    [Serializable]
    public class LivelySlider : IpcMessage
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double Step { get; set; }
        public LivelySlider() : base(MessageType.lp_slider)
        {
        }
    }

    [Serializable]
    public class LivelyTextBox : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyTextBox() : base(MessageType.lp_textbox)
        {
        }
    }

    [Serializable]
    public class LivelyDropdown : IpcMessage
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public LivelyDropdown() : base(MessageType.lp_dropdown)
        {
        }
    }

    [Serializable]
    public class LivelyFolderDropdown : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyFolderDropdown() : base(MessageType.lp_fdropdown)
        {
        }
    }

    [Serializable]
    public class LivelyCheckbox : IpcMessage
    {
        public string Name { get; set; }
        public bool Value { get; set; }
        public LivelyCheckbox() : base(MessageType.lp_chekbox)
        {
        }
    }

    [Serializable]
    public class LivelyColorPicker : IpcMessage
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public LivelyColorPicker() : base(MessageType.lp_cpicker)
        {
        }
    }

    [Serializable]
    public class LivelyButton : IpcMessage
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public LivelyButton() : base(MessageType.lp_button)
        {
        }
    }
}
