using ImageMagick;
using Lively.Common;
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
        private readonly string modelPath = Constants.MachineLearning.MiDaSPath;
        private readonly InferenceSession session;
        private readonly string inputName;
        private readonly int width, height;

        public MiDaS()
        {
            session = new InferenceSession(modelPath);
            Debug.WriteLine($"Model version: {session.ModelMetadata.Version}");

            inputName = session.InputMetadata.Keys.First();
            width = session.InputMetadata[inputName].Dimensions[2];
            height = session.InputMetadata[inputName].Dimensions[3];
        }

        public ModelOutput Run(string imagePath, string savePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException(imagePath);

            using var inputImage = new MagickImage(imagePath);
            var input = new ModelInput(imagePath, inputImage.Width, inputImage.Height);
            inputImage.Resize(new MagickGeometry(width, height) { IgnoreAspectRatio = true });

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
            var output = results.First().AsEnumerable<float>().ToArray();
            var normalisedOutput = NormaliseOutput(output);
            var result = new ModelOutput(normalisedOutput);

            using var outputImage = FloatArrayToMagickImage(normalisedOutput, width, height);
            outputImage.Resize(new MagickGeometry(input.Width, input.Height) { IgnoreAspectRatio = true });
            outputImage.Write(savePath);

            return result;
        }

        #region helpers

        private static MagickImage FloatArrayToMagickImage(float[] floatArray, int width, int height)
        {
            var image = new MagickImage(MagickColor.FromRgb(0, 0, 0), width, height);
            var pixels = image.GetPixels();
            for (int i = 0; i < floatArray.Length; i++)
            {
                pixels.SetPixel(i % width, i / width, new byte[] { (byte)(floatArray[i] * Quantum.Max), (byte)(floatArray[i] * Quantum.Max), (byte)(floatArray[i] * Quantum.Max) });
            }
            return image;
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

        #endregion //helpers
    }
}
