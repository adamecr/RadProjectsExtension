using System;
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

        /// <summary>
        /// Splits the string by first space and returns the "before" part.
        /// The "after" part is provided in output parameter <paramref name="rest"/>
        /// Both result and <paramref name="rest"/> are trimmed
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="rest">The part of string after the first space or null if there is no space</param>
        /// <returns>Part of the string before the first space or the whole string if no space detected</returns>
        public static string SplitByFirstSpace(this string input, out string rest)
        {
            input = input?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                rest = null;
                return null;
            }
            var spcIdx = input.IndexOf(" ", StringComparison.Ordinal);
            if (spcIdx < 0)
            {
                rest = null;
                return input;
            }
            rest = input.Substring(spcIdx + 1).Trim();
            return input.Substring(0, spcIdx).Trim();
        }

        /// <summary>
        /// Returns the part of the string after the last occurence of the <paramref name="separator"/>
        /// The result is always trimmed
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="separator">Part separator</param>
        /// <returns>Part of the string after the last occurence of the <paramref name="separator"/> or the input string when no separator detected</returns>
        public static string LastPart(this string input, string separator)
        {
            input = input?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            var spcIdx = input.LastIndexOf(separator, StringComparison.Ordinal);
            return spcIdx < 0 ? input.Trim() : input.Substring(spcIdx + 1).Trim();
        }
    }
}
