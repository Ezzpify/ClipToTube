using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace ClipToTube
{
    class Functions
    {
        /// <summary>
        /// Returns the current working directory
        /// </summary>
        /// <returns>Returns path string</returns>
        public static string GetAppFolder()
        {
            return Directory.GetCurrentDirectory() + "\\";
        }


        /// <summary>
        /// Returns a datetime timestmap
        /// </summary>
        /// <returns>Returns timestamp string</returns>
        public static string GetTimestamp()
        {
            return DateTime.Now.ToString("d/M/yyyy HH:mm:ss");
        }


        /// <summary>
        /// Extract all valid urls from a string
        /// </summary>
        /// <param name="str">String to extract from</param>
        /// <returns>Returns List<string></returns>
        public static List<string> ExtractLinks(string str)
        {
            var list = new List<string>();

            Regex linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match m in linkParser.Matches(str))
                list.Add(m.Value);

            return list;
        }


        /// <summary>
        /// Gets a string inbetween two strings
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="start">Start string</param>
        /// <param name="end">End string</param>
        /// <returns>Returns string if found</returns>
        public static string GetStringBetween(string source, string start, string end)
        {
            try
            {
                int startIndex = source.IndexOf(start);
                if (startIndex != -1)
                {
                    int endIndex = source.IndexOf(end, startIndex + 1);
                    if (endIndex != -1)
                    {
                        return source.Substring(startIndex + start.Length, endIndex - startIndex - start.Length);
                    }
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
