using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace livelywpf.Lively.Helpers
{
    public static class ImageOperations
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void ResizeImage(string srcFilePath,string saveFilePath, Size dimensions)
        {
            try
            {
                // Read from file
                using (MagickImage image = new MagickImage(srcFilePath))
                {
                    MagickGeometry size = new MagickGeometry(dimensions.Width, dimensions.Height);
                    // This will resize the image to a fixed size without maintaining the aspect ratio.
                    // Normally an image will be resized to fit inside the specified size.
                    size.IgnoreAspectRatio = true;

                    image.Resize(size);

                    // Save the result
                    image.Write(saveFilePath);
                }
            }
            catch(MagickException e)
            {
                Logger.Error(e.ToString());
            }
        }

        public static void ResizeGif(string srcFilePath, string saveFilePath, Size dimensions)
        {
            try
            {
                // Read from file
                using (MagickImageCollection collection = new MagickImageCollection(srcFilePath))
                {
                    // This will remove the optimization and change the image to how it looks at that point
                    // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                    collection.Coalesce();

                    // Resize each image in the collection to a width of 200. When zero is specified for the height
                    // the height will be calculated with the aspect ratio.
                    foreach (MagickImage image in collection)
                    {
                        image.Resize(dimensions.Width, dimensions.Height);
                    }

                    // Save the result
                    collection.Write(saveFilePath);
                }
            }
            catch(MagickException e)
            {
                Logger.Error(e.ToString());
            }
        }

        /// <summary>
        /// Adds image watermark over image.
        /// </summary>
        /// <param name="srcImagePath"></param>
        public static void SaveWaterMarkedImage(string srcImagePath, string waterMarkedSavePath, System.Drawing.Bitmap watermark, double opacity = 4)
        {
            try
            {
                using (MagickImage image = new MagickImage(srcImagePath))
                {
                    // Read the watermark that will be put on top of the image
                    using (MagickImage imageWatermark = new MagickImage(watermark))
                    {
                        // Draw the watermark in the bottom right corner
                        image.Composite(imageWatermark, Gravity.Northeast, CompositeOperator.Over);

                        // Optionally make the watermark more transparent
                        imageWatermark.Evaluate(Channels.Alpha, EvaluateOperator.Divide, opacity);

                        // Or draw the watermark at a specific location
                        //image.Composite(watermark, 200, 50, CompositeOperator.Over);
                    }

                    // Save the result
                    image.Write(waterMarkedSavePath);
                }
            }
            catch(MagickException e)
            {
                Logger.Error(e.ToString());
            }
        }
    }
}
