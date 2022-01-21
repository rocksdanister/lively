using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common
{
    public class Errors
    {
        public class WorkerWException : Exception
        {
            public WorkerWException()
            {
            }

            public WorkerWException(string message)
                : base(message)
            {
            }

            public WorkerWException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class WallpaperNotFoundException : Exception
        {
            public WallpaperNotFoundException()
            {
            }

            public WallpaperNotFoundException(string message)
                : base(message)
            {
            }

            public WallpaperNotFoundException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class WallpaperPluginException : Exception
        {
            public WallpaperPluginException()
            {
            }

            public WallpaperPluginException(string message)
                : base(message)
            {
            }

            public WallpaperPluginException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class WallpaperPluginNotFoundException : Exception
        {
            public WallpaperPluginNotFoundException()
            {
            }

            public WallpaperPluginNotFoundException(string message)
                : base(message)
            {
            }

            public WallpaperPluginNotFoundException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        /// <summary>
        /// Windows N/KN codec missing.
        /// </summary>
        public class WallpaperPluginMediaCodecException : Exception
        {
            public WallpaperPluginMediaCodecException()
            {
            }

            public WallpaperPluginMediaCodecException(string message)
                : base(message)
            {
            }

            public WallpaperPluginMediaCodecException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class WallpaperNotAllowedException : Exception
        {
            public WallpaperNotAllowedException()
            {
            }

            public WallpaperNotAllowedException(string message)
                : base(message)
            {
            }

            public WallpaperNotAllowedException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        public class ScreenNotFoundException : Exception
        {
            public ScreenNotFoundException()
            {
            }

            public ScreenNotFoundException(string message)
                : base(message)
            {
            }

            public ScreenNotFoundException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
