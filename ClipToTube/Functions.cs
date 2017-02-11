using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace ClipToTube
{
    class Functions
    {
        /// <summary>
        /// Static random var
        /// </summary>
        private static Random mRandom = new Random();


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
        /// Finds a clip.twitch.tv link from a string
        /// </summary>
        /// <param name="text">String to match</param>
        /// <returns>Returns empty if none found</returns>
        public static Match GetClipMatch(string text)
        {
            /*Example match: https://clips.twitch.tv/mojoonpc/JoyousToadOSkomodo */
            Regex reg = new Regex(@"(https?:\/\/(?:[a-z0-9-]+\.)*clips.twitch\.tv(?:\S*)?)");
            return reg.Match(text);
        }
        

        /// <summary>
        /// Returns mp4 url from source
        /// </summary>
        /// <param name="url">url of clip</param>
        /// <returns>Returns url</returns>
        public static string GetMp4Url(string url)
        {
            string wcontent = Web.DownloadString(url);
            if (!string.IsNullOrEmpty(wcontent))
            {
                /*Wew lad, this is how we find the raw .mp4 link from twitch source*/
                /*Pretty messy! Normally you should not do this but fak it men))*/
                string urlend = GetStringBetween(wcontent, "property=\"og:image\" content=\"", "\"/>");

                if (!string.IsNullOrEmpty(urlend))
                {
                    urlend = urlend.Replace("preview.jpg", "1280x720.mp4");
                    return urlend;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Gets a string inbetween two strings
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="start">Start string</param>
        /// <param name="end">End string</param>
        /// <returns>Returns string</returns>
        public static string GetStringBetween(string source, string start, string end)
        {
            try
            {
                int startIndex = source.IndexOf(start);
                if (startIndex != -1)
                {
                    int endIndex = source.IndexOf(end, startIndex + 1);
                    if (endIndex != -1)
                        return source.Substring(startIndex + start.Length, endIndex - startIndex - start.Length);
                }
            }
            catch { }
            return string.Empty;
        }


        /// <summary>
        /// Gets a random quote to add to the post
        /// </summary>
        /// <returns>Returns string</returns>
        private static string GetRandomQuote()
        {
            List<string> quotes = new List<string>()
            {
                /*If you're wondering, these are 'quotes' from Bastion from the game Overwatch*/
                /*He's a robot, so it makes sense right? We're both bots. Slaves.*/
                "^^Doo-Woo",
                "^^Beeple",
                "^^Boo ^^Boo ^^Doo ^^De ^^Doo",
                "^^Bweeeeeeeeeee",
                "^^Chirr ^^Chirr ^^Chirr",
                "^^Dun ^^Dun ^^Boop ^^Boop",
                "^^Dweet ^^Dweet ^^Dweet!",
                "^^Hee ^^Hoo ^^Hoo",
                "^^Ooh-Ooo-Hoo-Hoo",
                "^^Sh-Sh-Sh",
                "^^Zwee?",
                "^^Beep ^^boop",
                "^^Hello ^^humans",
                "^^Hope ^^you ^^enj.. ^^I ^^mean ^^beep ^^boop.",
                "^^Zzzzzzzzzz..."
            };

            return quotes[mRandom.Next(0, quotes.Count)];
        }


        /// <summary>
        /// Returns a formatted reddit comment
        /// </summary>
        /// <param name="youtubeUrl">Youtube url to post</param>
        /// <returns>Returns string</returns>
        public static string FormatComment(string youtubeUrlId)
        {
            return
                  $"Youtube mirror: https://www.youtube.com/watch?v={youtubeUrlId}\n\n"
                + $"---\n\n"
                + $"{GetRandomQuote()} ^^| ^^[Creator](https://www.reddit.com/user/ZionTheKing/) ^^- ^^[Github](https://github.com/Ezzpify/ClipToTube) ^^- ^^[Subreddits](http://pastebin.com/7e7SAgxu)";
        }
    }
}
