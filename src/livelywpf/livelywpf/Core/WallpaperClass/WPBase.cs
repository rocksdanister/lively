using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Core.WallpaperClass
{
    class WPBase
    {
        /// <summary>
        /// Window handle.
        /// </summary>
        public IntPtr Handle { get; set; } 
        //public string DisplayID { get; set; } 

        public WPBase(IntPtr handle, string displayID)
        {
            this.Handle = handle;
            //this.DisplayID = displayID;
        }
    }
}
