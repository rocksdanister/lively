using MathNet.Numerics.IntegralTransforms;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Lively.Common.Services
{
    //Ref: https://github.com/Quozul/Audio-Visualizer
    //MIT License Copyright (c) 2019 Erwan Le Gloannec.
    public class NAudioVisualizerService : IAudioVisualizerService, IMMNotificationClient
    {
        public event EventHandler<double[]> AudioDataAvailable;

        private readonly int maxSample = 128;
        private WasapiLoopbackCapture capture;
        private readonly List<Complex[]> smooth = new();
        private readonly static int vertical_smoothness = 2;
        private readonly static int horizontal_smoothness = 1;
        private readonly MMDeviceEnumerator deviceEnum = new();

        public NAudioVisualizerService()
        {
            try
            {
                var HRESULT = deviceEnum.RegisterEndpointNotificationCallback(this);
                if (HRESULT != 0)
                {
                    Debug.WriteLine("Failed to register audio device notifications.");
                }
                capture = CreateWasapiLoopbackCapture();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to initialize audio visualizer: {e.Message}");
            }
        }

        public void Start() => capture?.StartRecording();

        public void Stop() => capture?.StopRecording();

        private WasapiLoopbackCapture CreateWasapiLoopbackCapture(MMDevice device = null)
        {
            var tempCapture = device != null ? new WasapiLoopbackCapture(device) : new WasapiLoopbackCapture();
            tempCapture.DataAvailable += ProcessAudioData;
            tempCapture.RecordingStopped += (s, a) =>
            {
                tempCapture?.Dispose();
            };
            return tempCapture;
        }

        private void ProcessAudioData(object sender, WaveInEventArgs e)
        {
            try
            {
                var buffer = new WaveBuffer(e.Buffer); // save the buffer in the class variable

                int len = buffer.FloatBuffer.Length / 8;

                // fft
                var values = new Complex[len];
                for (int i = 0; i < len; i++)
                    values[i] = new Complex(buffer.FloatBuffer[i], 0.0);
                Fourier.Forward(values, FourierOptions.Default);

                // shift array
                smooth.Add(values);
                if (smooth.Count > vertical_smoothness)
                    smooth.RemoveAt(0);

                var audioData = new double[maxSample];
                for (int i = 0; i < maxSample; i++)
                {
                    audioData[i] = BothSmooth(i);
                }
                AudioDataAvailable?.Invoke(this, audioData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to process audio data: {ex.Message}");
            }
        }

        private double BothSmooth(int i)
        {
            var s = smooth.ToArray();
            double value = 0;
            for (int h = Math.Max(i - horizontal_smoothness, 0); h < Math.Min(i + horizontal_smoothness, maxSample); h++)
                value += VSmooth(h, s);

            return value / ((horizontal_smoothness + 1) * 2);
        }

        private static double VSmooth(int i, Complex[][] s)
        {
            double value = 0;

            for (int v = 0; v < s.Length; v++)
                value += Math.Abs(s[v] != null ? s[v][i].Magnitude : 0.0);

            return value / s.Length;
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render)
            {
                try
                {
                    capture?.StopRecording();
                    //var enumerator = new MMDeviceEnumerator();
                    //var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    capture = CreateWasapiLoopbackCapture();
                    capture.StartRecording();
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to update WasapiLoopbackCapture device: {e.Message}");
                }
            }
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Debug.WriteLine($"Device state changed: Device Id -> {deviceId} State -> {newState}");
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            Debug.WriteLine($"Device added: {pwstrDeviceId}");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            Debug.WriteLine($"Device removed: {deviceId}");
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            Debug.WriteLine($"Property Value Changed: formatId -> {key.formatId}  propertyId -> {key.propertyId}");
        }

        public void Dispose()
        {
            deviceEnum?.UnregisterEndpointNotificationCallback(this);
            Stop();
            //Calling dispose outside hangs.
            //capture?.Dispose();
        }
    }
}
