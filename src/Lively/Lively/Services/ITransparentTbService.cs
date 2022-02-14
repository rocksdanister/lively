using Lively.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Lively.Services
{
    public interface ITransparentTbService : IDisposable
    {
        bool IsRunning { get; }

        string CheckIncompatiblePrograms();
        System.Drawing.Color GetAverageColor(string filePath);
        void SetAccentColor(Color color);
        void Start(TaskbarTheme theme);
        void Stop();
    }
}
