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
        public class FileData
        {
            public List<string> Files { get; set; }
            public string ParentDirectory { get; set; }
        }

        /// <summary>
        /// Create zip from the list of folder(s).
        /// Example:
        /// List<string> folders = new List<string>();
        /// folders.Add("K:\\ziptest\\folder1");
        /// folders.Add("K:\\ziptest\\folder2");
        /// CreateZip("K:\\ziptest\\testzip.zip", folders);
        /// </summary>
        /// <param name="outPathname">Output zip filepath./</param>
        /// <param name="folders">Input folder(s) path(s).</param>
        public static void CreateZip(string outPathname, List<string> folders)
        {
            using (FileStream fsOut = File.Create(outPathname))
            using (var zipStream = new ZipOutputStream(fsOut))
            {
                //0-9, 9 being the highest level of compression
                zipStream.SetLevel(9);

                for (int i = 0; i < folders.Count; i++)
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

        /// <summary>
        /// Create zip from the list of files.
        /// Example:
        /// List<string> files1 = new List<string>() {"K:\\ziptest\\info\\f1.txt", "K:\\ziptest\\info\\subfolder\\f2.txt" };
        /// 
        /// List<string> files2 = new List<string>();
        /// files2.AddRange(Directory.GetFiles("K:\\ziptest\\folder\\", "*.*", SearchOption.AllDirectories));
        /// 
        /// List<ZipOperations.FileData> filesList = new List<ZipOperations.FileData>();
        /// filesList.Add(new ZipOperations.FileData() { files = files1, parentDirectory = "K:\\ziptest\\info" });
        /// filesList.Add(new ZipOperations.FileData() { files = files2, parentDirectory = "K:\\ziptest\\folder" });
        /// 
        /// ZipOperations.CreateZip("K:\\ziptest\\testzip.zip", filesList);
        /// </summary>
        /// <param name="outPathname"></param>
        /// <param name="fileData"></param>
        public static void CreateZip(string outPathname, List<FileData> fileData)
        {
            using (FileStream fsOut = File.Create(outPathname))
            using (var zipStream = new ZipOutputStream(fsOut))
            {
                //0-9, 9 being the highest level of compression
                zipStream.SetLevel(9);

                for (int i = 0; i < fileData.Count; i++)
                {
                    var item = fileData[i];
                    int folderOffset = item.ParentDirectory.Length + (item.ParentDirectory.EndsWith("\\") ? 0 : 1);
                    for (int j = 0; j < item.Files.Count; j++)
                    {
                        var file = item.Files[j];
                        var fi = new FileInfo(file);

                        //Debug.WriteLine(item + " " + folderOffset );
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
