using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Cef
{
    [Serializable]
    class FolderDropdown
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public object MenuItem { get; set; }

        public FolderDropdown(string fileName, string fullPath, string relativePath, object obj)
        {
            FileName = fileName;
            FullPath = fullPath;
            RelativePath = relativePath;
            MenuItem = obj;
        }
    }
}
