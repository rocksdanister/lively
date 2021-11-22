using System;
using System.Drawing;

namespace livelywpf.Services
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