// Copyright (c) 2020 Shankar
// The Shankar licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/ModernFlyouts-Community/ModernFlyouts

using System;
using System.Windows;
using observableObject = Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject;

namespace livelywpf.Core
{
    public class DisplayMonitor : observableObject
    {
        internal bool isStale;

        #region Properties

        private Rect bounds = Rect.Empty;

        public Rect Bounds
        {
            get => bounds;
            internal set => SetProperty(ref bounds, value);
        }

        private string deviceId = string.Empty;

        public string DeviceId
        {
            get => deviceId;
            internal set => SetProperty(ref deviceId, value);
        }

        public string DeviceName { get; }//{ get; init; }

        private string displayName = string.Empty;

        public string DisplayName
        {
            get => displayName;
            internal set => SetProperty(ref displayName, value);
        }

        private IntPtr hMonitor = IntPtr.Zero;

        public IntPtr HMonitor
        {
            get => hMonitor;
            internal set => SetProperty(ref hMonitor, value);
        }

        private int index;

        public int Index
        {
            get => index;
            internal set => SetProperty(ref index, value);
        }

        private bool isPrimary;

        public bool IsPrimary
        {
            get => isPrimary;
            internal set => SetProperty(ref isPrimary, value);
        }

        private Rect workingArea = Rect.Empty;

        public Rect WorkingArea
        {
            get => workingArea;
            internal set => SetProperty(ref workingArea, value);
        }

        #endregion

        internal DisplayMonitor(string deviceName)
        {
            DeviceName = deviceName;
        }
    }
}
