using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Helpers
{
    public static class BuildInfoUtil
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3400:Methods should not return constants", Justification = "Value changes based on build setting.")]
        public static bool IsDebugBuild()
        {
#if DEBUG
            return true;
#else
        return false;
#endif
        }
    }
}
