using Lively.Models.Enums;
using System;

namespace Lively.Common.Services
{
    public interface ISystray : IDisposable
    {
        void SetTheme(AppTheme theme);
        void ShowBalloonNotification(int timeout, string title, string msg);
        void Visibility(bool visible);
    }
}