using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.Helpers.Archive
{
    public static class ZipExtract
    {

        /// <summary>
        /// Extract zip file to given output directory.
        /// </summary>
        /// <param name="archivePath">Source .zip path.</param>
        /// <param name="outFolder">Destination directory.</param>
        /// <param name="livelyFileCheck">Verify whether the archive is lively wallpaper format, throws Exception if not.</param>
        public static void ZipExtractFile(string archivePath, string outFolder, bool livelyFileCheck)
        {
            using (Stream fsInput = File.OpenRead(archivePath))
            using (var zf = new ZipFile(fsInput))
            {

                if (livelyFileCheck && zf.FindEntry("LivelyInfo.json", true) == -1)
                {
                    throw new Exception("LivelyInfo.json not found");
                }

                //long i = 0;
                foreach (ZipEntry zipEntry in zf)
                {
                    //progress
                    //float percentage = (float)++i / zf.Count;
                    //Debug.WriteLine(percentage + " " + zipEntry.Name);

                    if (!zipEntry.IsFile)
                    {
                        // Ignore directories
                        continue;
                    }

                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:
                    //entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here
                    // to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var zipStream = zf.GetInputStream(zipEntry))
                    using (Stream fsOutput = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, fsOutput, buffer);
                    }
                }
            }
        }

        /// <summary>
        /// Verify whether the archive is lively wallpaper format.
        /// </summary>
        /// <param name="archivePath">Path to .zip file.</param>
        /// <returns></returns>
        public static bool CheckLivelyZip(string archivePath)
        {
            bool result = true;
            try
            {
                using (Stream fsInput = File.OpenRead(archivePath))
                using (var zf = new ZipFile(fsInput))
                {

                    if (zf.FindEntry("LivelyInfo.json", true) == -1)
                    {
                        result = false;
                    }
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }
    }
}
