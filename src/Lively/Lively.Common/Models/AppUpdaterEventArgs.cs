using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Lively.Common.Models
{
    public class AppUpdaterEventArgs : EventArgs
    {
        public AppUpdaterEventArgs(AppUpdateStatus updateStatus, Version updateVersion, DateTime updateDate, Uri updateUri, string fileName)
        {
            this.UpdateStatus = updateStatus;
            this.UpdateVersion = updateVersion;
            this.UpdateUri = updateUri;
            UpdateDate = updateDate;
            this.FileName = fileName;
        }

        public AppUpdateStatus UpdateStatus { get; }
        public Version UpdateVersion { get; }
        public Uri UpdateUri { get; }
        public DateTime UpdateDate { get; }
        public string FileName { get; }
    }

    public enum AppUpdateStatus
    {
        [Description("Software is up-to-date.")]
        uptodate,
        [Description("Update available.")]
        available,
        [Description("Installed software version higher than whats available online.")]
        invalid,
        [Description("Update not checked yet.")]
        notchecked,
        [Description("Update check failed.")]
        error,
    }
}
