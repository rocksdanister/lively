/* 
Credit:
https://github.com/ModernFlyouts-Community/ModernFlyouts

MIT License

Copyright (c) 2020 Shankar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
