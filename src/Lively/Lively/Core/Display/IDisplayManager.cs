// Copyright (c) 2020 Shankar
// The Shankar licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/ModernFlyouts-Community/ModernFlyouts

using Lively.Models;
using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Lively.Core.Display
{
    public interface IDisplayManager
    {
        ObservableCollection<DisplayMonitor> DisplayMonitors { get; }
        DisplayMonitor PrimaryDisplayMonitor { get; }
        Rectangle VirtualScreenBounds { get; }

        event EventHandler DisplayUpdated;

        DisplayMonitor GetDisplayMonitorFromHWnd(IntPtr hWnd);
        DisplayMonitor GetDisplayMonitorFromPoint(Point point);
        bool IsMultiScreen();
        uint OnHwndCreated(IntPtr hWnd, out bool register);
        IntPtr OnWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        bool ScreenExists(IDisplayMonitor display);
    }
}