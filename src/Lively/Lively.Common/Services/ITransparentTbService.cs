using Lively.Models.Enums;
using System;
using System.Drawing;

namespace Lively.Common.Services
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
