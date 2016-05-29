namespace ClipToTube
{
    class Endpoints
    {
        /// <summary>
        /// Path to client secret file
        /// </summary>
        public static string YOUTUBE_CLIENT_SECRET = Functions.GetAppFolder() + "Settings\\client_secret.json";


        /// <summary>
        /// Path to checked posts file
        /// </summary>
        public static string CHECKED_POSTS_FILE = Functions.GetAppFolder() + "Settings\\CheckedPosts.json";


        /// <summary>
        /// Path to settings file
        /// </summary>
        public static string SETTINGS_FILE = Functions.GetAppFolder() + "Settings\\Settings.json";
    }
}
