using ImageMagick;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.ML.DepthEstimate
{
    public class MiDaS : IDepthEstimate
    {
        public string ModelPath { get; private set; }
        private InferenceSession session;
        private string inputName;
        private int width, height;
        private bool disposedValue;

        public MiDaS() { }

        public void LoadModel(string path)
        {
            ModelPath = path;

            session?.Dispose();
            session = new InferenceSession(ModelPath);
            Debug.WriteLine($"Model version: {session.ModelMetadata.Version}");

            inputName = session.InputMetadata.Keys.First();
            width = session.InputMetadata[inputName].Dimensions[2];
            height = session.InputMetadata[inputName].Dimensions[3];
        }

        public ModelOutput Run(string imagePath)
        {
            if (ModelPath is null)
                throw new FileNotFoundException("ONNX file not provided");

            if (!File.Exists(imagePath))
                throw new FileNotFoundException(imagePath);

            using var inputImage = new MagickImage(imagePath);
            var inputModel = new ModelInput(imagePath, (int)inputImage.Width, (int)inputImage.Height);
            inputImage.Resize(new MagickGeometry((uint)width, (uint)height) { IgnoreAspectRatio = true });

            var t1 = new DenseTensor<float>(new[] { 1, 3, height, width });
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    using var pixels = inputImage.GetPixels();
                    var color = pixels.GetPixel(x, y).ToColor();
                    t1[0, 0, y, x] = color.R / 255f;
                    t1[0, 1, y, x] = color.G / 255f;
                    t1[0, 2, y, x] = color.B / 255f;
                }
            }

            var inputs = new List<NamedOnnxValue>() {
                NamedOnnxValue.CreateFromTensor<float>(inputName, t1),
            };

            using var results = session.Run(inputs);
            var outputModel = results.First().AsEnumerable<float>().ToArray();
            var normalisedOutput = NormaliseOutput(outputModel);

            return new ModelOutput(normalisedOutput, width, height,inputModel.Width, inputModel.Height);
        }

        private static float[] NormaliseOutput(float[] data)
        {
            var depthMax = data.Max();
            var depthMin = data.Min();
            var depthRange = depthMax - depthMin;

            var normalisedOutput = data.Select(d => (d - depthMin) / depthRange)
                .Select(n => ((1f - n) * 0f + n * 1f)).ToArray();
            return normalisedOutput;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    session?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MiDaS()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
