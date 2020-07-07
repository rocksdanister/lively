using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace livelywpf
{
    public static class ZipCreate
    {
        /// <summary>
        /// List<string> folders = new List<string>();
        /// folders.Add(@"K:\ziptest\info");
        /// folders.Add(@"K:\ziptest\extracted");
        /// CreateZip(@"K:\ziptest\testzip.zip", folders.ToArray());
        /// </summary>
        /// <param name="outPathname">Output zip filepath./</param>
        /// <param name="folders">Input folder(s) path(s).</param>
        public static void CreateZip(string outPathname, string[] folders)
        {
            using (FileStream fsOut = File.Create(outPathname))
            using (var zipStream = new ZipOutputStream(fsOut))
            {
                //0-9, 9 being the highest level of compression
                zipStream.SetLevel(9);

                for (int i = 0; i < folders.Length; i++)
                {
                    var folder = folders[i];
                    int folderOffset = folder.Length + (folder.EndsWith("\\") ? 0 : 1);
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                    for (int j = 0; j < files.Length; j++)
                    {
                        var file = files[j];
                        var fi = new FileInfo(file);

                        // Make the name in zip based on the folder
                        var entryName = file.Substring(folderOffset);

                        // Remove drive from name and fix slash direction
                        entryName = ZipEntry.CleanName(entryName);

                        var newEntry = new ZipEntry(entryName);

                        // Note the zip format stores 2 second granularity
                        newEntry.DateTime = fi.LastWriteTime;

                        // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003,
                        // WinZip 8, Java, and other older code, you need to do one of the following: 
                        // Specify UseZip64.Off, or set the Size.
                        // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, 
                        // you do not need either, but the zip will be in Zip64 format which
                        // not all utilities can understand.
                        // zipStream.UseZip64 = UseZip64.Off;
                        newEntry.Size = fi.Length;

                        zipStream.PutNextEntry(newEntry);

                        // Zip the file in buffered chunks
                        // the "using" will close the stream even if an exception occurs
                        var buffer = new byte[4096];
                        using (FileStream fsInput = File.OpenRead(file))
                        {
                            StreamUtils.Copy(fsInput, zipStream, buffer);
                        }
                        zipStream.CloseEntry();
                    }
                }
            }
        }
    }
}
