using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.ML.Helpers
{
    public static class ImageUtil
    {
        /// <summary>
        /// Converts 1D FloatArray to ImageMagick format
        /// </summary>
        public static MagickImage FloatArrayToMagickImage(float[] floatArray, int width, int height)
        {
            var image = new MagickImage(MagickColor.FromRgb(0, 0, 0), (uint)width, (uint)height);
            var pixels = image.GetPixels();
            for (int i = 0; i < floatArray.Length; i++)
            {
                pixels.SetPixel(i % width, i / width, new byte[] { (byte)(floatArray[i] * Quantum.Max), (byte)(floatArray[i] * Quantum.Max), (byte)(floatArray[i] * Quantum.Max) });
            }
            return image;
        }

        /// <summary>
        /// Converts 1D FloatArray to ImageMagick format and resize
        /// </summary>
        public static MagickImage FloatArrayToMagickImageResize(float[] floatArray, int width, int height, int widthResize, int heightResize)
        {
            var image = FloatArrayToMagickImage(floatArray, width, height);
            image.Resize(new MagickGeometry((uint)widthResize, (uint)heightResize) { IgnoreAspectRatio = true });
            return image;
        }
    }
}
