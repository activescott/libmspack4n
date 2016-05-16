using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibMSPackN
{
    internal class PathEx
    {
        /// <summary>
        /// This is the special prefix to prepend to paths to support up to 32,767 character paths.
        /// </summary>
        public static readonly string LongPathPrefix = @"\\?\";

        /// <summary>
        /// Returns the specified path with the <see cref="LongPathPrefix"/> prepended if necessary.
        /// </summary>
        public static string EnsureLongPathPrefix(string path)
        {
            if (!path.StartsWith(LongPathPrefix)) // More consistent to deal with if we just add it to all of them: if (!path.StartsWith(LongPathPrefix) && path.Length >= MAX_PATH)
                return LongPathPrefix + path;
            else
                return path;
        }
    }
}
