﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services.Audio
{
    public interface IAudioVisualizerService : IDisposable
    {
        event EventHandler<double[]> AudioDataAvailable;
        void Start();
        void Stop();
    }
}
