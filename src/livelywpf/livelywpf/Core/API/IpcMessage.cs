using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Core.API
{
    public enum MessageType
    {
        cmd_reload,
        cmd_close,
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

    [Serializable]
    public abstract class IpcMessage
    {
        public MessageType Type { get; }
        public IpcMessage(MessageType type)
        {
            this.Type = type;
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
