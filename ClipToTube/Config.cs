using System.Collections.Generic;
using RedditSharp.Things;

namespace ClipToTube
{
    class Config
    {
        public class Settings
        {
            /// <summary>
            /// Reddit account information
            /// </summary>
            public RedditAccount reddit { get; set; } = new RedditAccount();


            /// <summary>
            /// List of subreddits we'll be working in
            /// </summary>
            public List<string> subredditList { get; set; } = new List<string>();
        }


        /// <summary>
        /// Class holding reddit account information
        /// </summary>
        public class RedditAccount
        {
            /// <summary>
            /// Reddit username
            /// </summary>
            public string username { get; set; } = string.Empty;


            /// <summary>
            /// Reddit password
            /// </summary>
            public string password { get; set; } = string.Empty;
        }


        /// <summary>
        /// Class holding clip information
        /// </summary>
        public class Clip
        {
            /// <summary>
            /// Local path to clip
            /// </summary>
            public string filepath { get; set; }


            /// <summary>
            /// clips.twitch.tv url
            /// </summary>
            public string clipUrl { get; set; }


            /// <summary>
            /// Reddit post
            /// </summary>
            public Post post { get; set; }
        }
    }
}
