using System.IO;

namespace net.adamec.dev.vs.extension.radprojects.utils
{
    /// <summary>
    /// String extensions class
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Adds next path segment to string representing path to the file system
        /// </summary>
        /// <param name="str">String representing path to the file system</param>
        /// <param name="nextSegment">Next path segment</param>
        /// <returns>String representing path to the file system</returns>
        public static string AddPath(this string str,string nextSegment)
        {
            return Path.Combine(str, nextSegment);
        }
        
    }
}
