/*System*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;

/*Reddit*/
using RedditSharp;
using RedditSharp.Things;

/*Youtube*/
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace ClipToTube
{
    class Session
    {
        /// <summary>
        /// List of reddit posts we've already checked/dealt with
        /// </summary>
        public List<string> mCheckedPosts = new List<string>();


        /// <summary>
        /// Main background worker
        /// </summary>
        private BackgroundWorker mBackgroundWork;


        /// <summary>
        /// Logs
        /// </summary>
        private Log mLog, mLogError, mLogYoutube;


        /// <summary>
        /// Application settings
        /// </summary>
        private Config.Settings mSettings;


        /// <summary>
        /// Youtube service connection
        /// </summary>
        private YouTubeService mYoutube;


        /// <summary>
        /// Reddit connection
        /// </summary>
        private Reddit mReddit;


        /// <summary>
        /// Session constructor
        /// </summary>
        /// <param name="settings">Application settings class</param>
        public Session(Config.Settings settings)
        {
            mSettings = settings;

            if (File.Exists(Endpoints.CHECKED_POSTS_FILE))
                mCheckedPosts = JsonConvert.DeserializeObject<List<string>>
                    (File.ReadAllText(Endpoints.CHECKED_POSTS_FILE));
            
            if (AuthenticateReddit())
                Console.WriteLine("Reddit authenticated");
            
            AuthenticateYoutube().Wait();
            if (mYoutube != null)
                Console.WriteLine("Youtube authenticated");

            mLog = new Log("Session", "Logs\\Session.txt", 3);
            mLogError = new Log("Error", "Logs\\ErrorLogs\\Error.txt", 3);
            mLogYoutube = new Log("Youtube", "Logs\\Youtube.txt", 3);

            mBackgroundWork = new BackgroundWorker();
            mBackgroundWork.WorkerSupportsCancellation = true;
            mBackgroundWork.DoWork += MBackgroundWork_DoWork;
            mBackgroundWork.RunWorkerCompleted += MBackgroundWork_RunWorkerCompleted;
            mBackgroundWork.RunWorkerAsync();

            while (true)
                Thread.Sleep(500);
        }


        /// <summary>
        /// Main worker
        /// </summary>
        private void MBackgroundWork_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!mBackgroundWork.CancellationPending)
            {
                foreach (var subredditname in mSettings.subredditList)
                {
                    Subreddit subreddit;

                    try
                    {
                        subreddit = mReddit.GetSubreddit(subredditname);
                    }
                    catch (Exception ex)
                    {
                        mLogError.Write(Log.LogLevel.Error, $"Error getting Subreddit\n{ex.Message}");
                        Thread.Sleep(5000);
                        continue;
                    }

                    List<Post> posts = new List<Post>();
                    posts.AddRange(subreddit.New.Take(30));
                    posts.AddRange(subreddit.Rising.Take(15));

                    foreach (var post in posts)
                    {
                        try
                        {
                            if (mCheckedPosts.Contains(post.Id) || post.Upvotes <= 2 || !post.IsSelfPost)
                                continue;

                            mLog.Write(Log.LogLevel.Info, $"Checking post {post.Id} ({post.Title}) ({post.Shortlink})");
                            
                            var clipUrl = Functions.ExtractLinks(post.SelfText)
                                .FirstOrDefault(o => o.Contains("clips.twitch.tv"));

                            if (!string.IsNullOrEmpty(clipUrl))
                            {
                                mLog.Write(Log.LogLevel.Success, $"Found a twitch clip link: {clipUrl}");
                                string filePath = Web.DownloadFile(GetMp4Url(clipUrl));

                                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                                {
                                    mLog.Write(Log.LogLevel.Success, $"Downloaded file! Proceeding to upload file.");
                                    Upload(filePath, post.Title, post.Url.ToString(), post.Id).Wait();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            mLogError.Write(Log.LogLevel.Error, $"Something happened when checking post {post.Id}\n{ex}");
                        }

                        mCheckedPosts.Add(post.Id);
                        SaveAllCheckedPosts();
                    }
                }

                Thread.Sleep(TimeSpan.FromMinutes(15));
            }
        }


        /// <summary>
        /// Saves all posts checked to file
        /// </summary>
        private void SaveAllCheckedPosts()
        {
            if (mCheckedPosts.Count > 175)
                mCheckedPosts.RemoveRange(0, 25);

            File.WriteAllText(Endpoints.CHECKED_POSTS_FILE,
                JsonConvert.SerializeObject(mCheckedPosts, Formatting.Indented));
        }


        /// <summary>
        /// Returns mp4 url from source
        /// </summary>
        /// <param name="url">url of clip</param>
        /// <returns>Returns url</returns>
        private string GetMp4Url(string url)
        {
            string wcontent = Web.DownloadString(url);
            string urlend = Functions.GetStringBetween(wcontent, "clip_video_url: \"", "\",").Replace("\\", "");
            
            if (!string.IsNullOrEmpty(urlend))
                return urlend;

            return string.Empty;
        }


        /// <summary>
        /// Uploads a video to Youtube
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        private async Task Upload(string filepath, string title, string description, string redditurl)
        {
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = title;
            video.Snippet.Description = description;
            video.Snippet.Tags = new string[] { "Hello" };
            video.Snippet.CategoryId = "20";

            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = "public"; // or "private" or "unlisted"

            using (var fileStream = new FileStream(filepath, FileMode.Open))
            {
                var videosInsertRequest = mYoutube.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += (e) => { videosInsertRequest_ResponseReceived(e, redditurl); };

                await videosInsertRequest.UploadAsync();
            }
        }


        /// <summary>
        /// Youtube upload progress changed
        /// </summary>
        /// <param name="progress"></param>
        void videosInsertRequest_ProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    mLog.Write(Log.LogLevel.Info, $"{progress.BytesSent} bytes sent.");
                    break;

                case UploadStatus.Failed:
                    mLog.Write(Log.LogLevel.Info, $"An error prevented the upload from completing.\n{progress.Exception}");
                    break;
            }
        }


        /// <summary>
        /// Youtube upload complete
        /// </summary>
        /// <param name="video"></param>
        void videosInsertRequest_ResponseReceived(Video video, string redditurl)
        {
            mLog.Write(Log.LogLevel.Success, $"Video id {video.Id} was successfully uploaded!");

            var post = mReddit.GetPost(new Uri($"https://www.reddit.com/{redditurl}/"));
            if (post != null)
            {
                string comment = 
                     $"Youtube mirror: https://www.youtube.com/watch?v={video.Id}\n\n"
                    + "---\n\n"
                    + "^^Bot ^^creator: ^^https://www.reddit.com/user/ZionTheKing/";

                post.Comment(comment);
                mLog.Write(Log.LogLevel.Success, $"Comment posted to thread {redditurl}");
            }
        }


        /// <summary>
        /// Main worker completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MBackgroundWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                mLogError.Write(Log.LogLevel.Error, $"An unhandled exception caused mBackgroundWork to crash.\n{e.Error}");

            mLog.Write(Log.LogLevel.Info, $"Worker has exited.");
        }


        /// <summary>
        /// Authenticates our reddit account
        /// </summary>
        /// <returns></returns>
        private bool AuthenticateReddit()
        {
            mReddit = new Reddit(mSettings.reddit.username, mSettings.reddit.password);
            return mReddit.User != null;
        }


        /// <summary>
        /// Authenticates our youtube account
        /// </summary>
        /// <returns></returns>
        private async Task AuthenticateYoutube()
        {
            if (!File.Exists(Endpoints.YOUTUBE_CLIENT_SECRET))
            {
                Console.WriteLine("Missing client_secret.json!\nCreate a youtube project and place the client_secret.json in Settings folder.");
                Process.Start("https://console.developers.google.com/apis/credentials?project=");
                return;
            }

            UserCredential credential;
            using (var stream = new FileStream(Endpoints.YOUTUBE_CLIENT_SECRET, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeUpload },
                    "user",
                    CancellationToken.None
                );
            }

            mYoutube = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ClipToTube"
            });
        }
    }
}
