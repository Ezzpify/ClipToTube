using System.Collections.Generic;

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
    }
}
