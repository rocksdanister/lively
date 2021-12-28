// Copyright (c) 2020 Shankar
// The Shankar licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/ModernFlyouts-Community/ModernFlyouts

using System;
using System.Drawing;

namespace Lively.Models
{
    public interface IDisplayMonitor : IEquatable<IDisplayMonitor>
    {
        Rectangle Bounds { get; }
        string DeviceId { get; }
        string DeviceName { get; }
        string DisplayName { get; }
        IntPtr HMonitor { get; }
        int Index { get; }
        bool IsPrimary { get; }
        Rectangle WorkingArea { get; }
    }
}