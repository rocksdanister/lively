// Copyright (c) 2020 Shankar
// The Shankar licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/ModernFlyouts-Community/ModernFlyouts

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Drawing;

namespace Lively.Models
{
    public sealed class DisplayMonitor : ObservableObject, IEquatable<DisplayMonitor>
    {
        public bool isStale;

        #region Properties

        private Rectangle bounds = Rectangle.Empty;

        public Rectangle Bounds
        {
            get => bounds;
            set => SetProperty(ref bounds, value);
        }

        private string deviceId = string.Empty;

        public string DeviceId
        {
            get => deviceId;
            set => SetProperty(ref deviceId, value);
        }

        private string deviceName = string.Empty;
        public string DeviceName
        {
            get => deviceName;
            set => SetProperty(ref deviceName, value);
        }

        private string displayName = string.Empty;

        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        private IntPtr hMonitor = IntPtr.Zero;

        public IntPtr HMonitor
        {
            get => hMonitor;
            set => SetProperty(ref hMonitor, value);
        }

        private int index;

        public int Index
        {
            get => index;
            set => SetProperty(ref index, value);
        }

        private bool isPrimary;

        public bool IsPrimary
        {
            get => isPrimary;
            set => SetProperty(ref isPrimary, value);
        }

        private Rectangle workingArea = Rectangle.Empty;

        public Rectangle WorkingArea
        {
            get => workingArea;
            set => SetProperty(ref workingArea, value);
        }

        #endregion

        public DisplayMonitor(string deviceName)
        {
            DeviceName = deviceName;
        }

        public DisplayMonitor() { }

        public bool Equals(DisplayMonitor other)
        {
            return other.DeviceId == this.DeviceId;
        }
    }
}
