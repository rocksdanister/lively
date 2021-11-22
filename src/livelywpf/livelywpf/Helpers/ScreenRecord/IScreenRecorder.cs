using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace livelywpf.Helpers.ScreenRecord
{
    public enum ScreenRecorderStatus
    {
        idle, 
        paused,
        fail, 
        recording,
        finishing,
        success
    }

    public interface IScreenRecorder
    {
        /// <summary>
        /// Initialize video capture instance.
        /// </summary>
        /// <param name="filePath">Save path of mp4 file.</param>
        /// <param name="rect">Capture position.</param>
        /// <param name="fps">frame rate.</param>
        /// <param name="bitrate">video bitrate.</param>
        /// <param name="isAudio">Capture audio.</param>
        /// <param name="isMousePointer">Capture mouse cursor.</param>
        void Initialize(string filePath, Rect rect, int fps, int bitrate, bool isAudio, bool isMousePointer);
        void Initialize(string filePath, IntPtr hwnd, int fps, int bitrate, bool isAudio, bool isMousePointer);
        /// <summary>
        /// Start video capture.
        /// </summary>
        void StartRecording();
        /// <summary>
        /// Stop video capture.
        /// </summary>
        void StopRecording();
        /// <summary>
        /// Current recording instance status.
        /// </summary>
        event EventHandler<ScreenRecorderStatus> RecorderStatus;
    }
}
