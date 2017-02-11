using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using RedditSharp;
using RedditSharp.Things;

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
        /// Main background worker
        /// </summary>
        private BackgroundWorker mBackgroundWork = new BackgroundWorker();


        /// <summary>
        /// List of reddit posts we've already checked/dealt with
        /// </summary>
        private List<string> mCheckedPosts = new List<string>();


        /// <summary>
        /// Application settings
        /// </summary>
        private Config.Settings mSettings;


        /// <summary>
        /// Youtube service connection
        /// </summary>
        private YouTubeService mYoutube;


        /// <summary>
        /// Logs
        /// </summary>
        private Log mLog, mLogError;


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

            /*Load the file containing all already checked reddit posts*/
            if (File.Exists(Endpoints.CHECKED_POSTS_FILE))
                mCheckedPosts = JsonConvert.DeserializeObject<List<string>>
                    (File.ReadAllText(Endpoints.CHECKED_POSTS_FILE));

            /*Attempt to authenticate our Reddit account*/
            mReddit = new Reddit(mSettings.reddit.username, mSettings.reddit.password);
            if (mReddit == null)
            {
                Console.WriteLine("Reddit authentication failed");
                return;
            }

            /*Attempt to authenticate our YouTube account*/
            AuthenticateYoutube().Wait();
            if (mYoutube == null)
            {
                Console.WriteLine("Youtube authentication failed");
                return;
            }

            mLogError = new Log("Error", "Logs\\ErrorLogs\\Error.txt", 3);
            mLog = new Log("Session", "Logs\\Session.txt", 3);
            
            mBackgroundWork.WorkerSupportsCancellation = true;
            mBackgroundWork.DoWork += MBackgroundWork_DoWork;
            mBackgroundWork.RunWorkerCompleted += MBackgroundWork_RunWorkerCompleted;
            mBackgroundWork.RunWorkerAsync();

            while (mBackgroundWork.IsBusy)
                /*Keep alive*/
                Thread.Sleep(500);
        }


        /// <summary>
        /// Main worker
        /// </summary>
        private void MBackgroundWork_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!mBackgroundWork.CancellationPending)
            {
                List<Config.Clip> clipQueue = new List<Config.Clip>();
                foreach (var subredditname in mSettings.subredditList)
                {
                    Subreddit subreddit = TryGetSubreddit(subredditname);
                    if (subreddit == null)
                        continue;
                    
                    /*Take posts from both new and rising*/
                    List<Post> posts = new List<Post>();
                    posts.AddRange(subreddit.New.Take(30));
                    posts.AddRange(subreddit.Rising.Take(15));

                    /*Remove post duplicates and remove posts we've already checked*/
                    posts = posts.GroupBy(o => o.Id).Select(o => o.First()).ToList();
                    posts.RemoveAll(p => mCheckedPosts.Any(o => o == p.Id));
                    mLog.Write(Log.LogLevel.Info, $"Checking {posts.Count} posts at /r/{subredditname}");

                    foreach (var post in posts)
                    {
                        try
                        {
                            /*If we already checked this post*/
                            if (mCheckedPosts.Contains(post.Id))
                                continue;

                            /*Add this post's id to already checked list so it won't be checked again*/
                            mCheckedPosts.Add(post.Id);

                            /*Try to match first if we can find a link in the title*/
                            Match regMatch = Functions.GetClipMatch(post.Url.ToString());

                            /*Couldn't find a match in the title, if the post is a selfpost we'll check the text there*/
                            if (!regMatch.Success && post.IsSelfPost)
                                regMatch = Functions.GetClipMatch(post.SelfText);

                            if (!regMatch.Success || string.IsNullOrEmpty(regMatch.Value))
                                continue;

                            /*We found a link, add it to queue and keep searching*/
                            mLog.Write(Log.LogLevel.Success, $"Adding a clips.twitch.tv link to queue ({post.Id} ({regMatch.Value})");
                            clipQueue.Add(new Config.Clip() { post = post, clipUrl = regMatch.Value });
                        }
                        catch (Exception ex)
                        {
                            mLogError.Write(Log.LogLevel.Error, $"Something happened when checking post {post.Id}\n{ex}");
                        }
                    }
                    
                    /*Save all checked posts and wait before checking next subreddit*/
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                UploadClips(clipQueue);
                SaveAllCheckedPosts();
            }
        }


        /// <summary>
        /// Try to get subreddit by name
        /// </summary>
        /// <param name="name">Name of subreddit</param>
        /// <returns>Returns null if failed, else Subreddit</returns>
        private Subreddit TryGetSubreddit(string name)
        {
            Subreddit subreddit = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    /*Try to get the sub-reddit which we will retreive posts from*/
                    subreddit = mReddit.GetSubreddit(name);
                    if (subreddit != null)
                        break;
                }
                catch (Exception ex)
                {
                    mLogError.Write(Log.LogLevel.Error, $"Error getting /r/{name}\n{ex.Message}");
                    Thread.Sleep(5000);
                }
            }

            return subreddit;
        }


        /// <summary>
        /// Uploads all the clips queued
        /// </summary>
        /// <param name="clips">List of clips</param>
        private void UploadClips(List<Config.Clip> clips)
        {
            /*Log the datetime when we completed the search*/
            /*Now we want to start uploading the video files to youtube*/
            DateTime checkedPostTime = DateTime.Now;
            foreach (var clip in clips)
            {
                string clipUrl = Functions.GetMp4Url(clip.clipUrl);
                clip.filepath = Web.DownloadFile(clipUrl);
                if (!string.IsNullOrEmpty(clip.filepath))
                {
                    mLog.Write(Log.LogLevel.Success, $"Downloaded clip! Proceeding to upload. (Title: {clip.post.Title})");
                    Upload(clip).Wait();
                }
            }

            /*By default we don't want to check reddit too often*/
            /*However, if the upload queue is long we don't want to sleep for additional time*/
            /*So we'll take the default time we would normally sleep, then subtract the time it took to upload the files*/
            TimeSpan uploadTime = DateTime.Now - checkedPostTime;

            /*If we still need to sleep (eg. the upload time didn't take longer than the default sleep time) then we will sleep here*/
            TimeSpan sleepTime = new TimeSpan(0 /*hours*/, 8 /*minutes*/, 0 /*seconds*/) - uploadTime;
            if (sleepTime.TotalMilliseconds > 0)
            {
                mLog.Write(Log.LogLevel.Info, $"Sleeping for {Math.Round(sleepTime.TotalSeconds)} Seconds.");
                Thread.Sleep(sleepTime);
            }
        }


        /// <summary>
        /// Saves all posts checked to file
        /// </summary>
        private void SaveAllCheckedPosts()
        {
            /*Only really need to save the last 1000 reddit links*/
            /*Realistically I should have a database for this, but for now it will do*/
            if (mCheckedPosts.Count > 1100)
                mCheckedPosts.RemoveRange(0, 100);

            File.WriteAllText(Endpoints.CHECKED_POSTS_FILE,
                JsonConvert.SerializeObject(mCheckedPosts, Formatting.Indented));
        }


        /// <summary>
        /// Uploads a video to youtube
        /// </summary>
        /// <param name="clip">Clip class containing file information</param>
        /// <returns>Returns task</returns>
        private async Task Upload(Config.Clip clip)
        {
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = clip.post.Title;
            video.Snippet.Description = clip.post.Url.ToString();
            video.Snippet.Tags = new string[] { "twitch", "tv" };
            video.Snippet.CategoryId = "20";

            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = "public";

            using (var fileStream = new FileStream(clip.filepath, FileMode.Open))
            {
                /*Upload the video to youtube*/
                var videosInsertRequest = mYoutube.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += (e) => { videosInsertRequest_ResponseReceived(e, clip.post.Id); };

                await videosInsertRequest.UploadAsync();
            }
        }


        /// <summary>
        /// Youtube upload progress changed
        /// </summary>
        /// <param name="progress">Upload progress</param>
        void videosInsertRequest_ProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    mLog.Write(Log.LogLevel.Success, $"{(progress.BytesSent / 1024f) / 1024f /*Convert from bytes to megabytes*/} MB sent.");
                    break;

                case UploadStatus.Failed:
                    mLog.Write(Log.LogLevel.Error, $"An error prevented the upload from completing.\n{progress.Exception}");
                    break;
            }
        }


        /// <summary>
        /// Youtube upload complete event
        /// </summary>
        /// <param name="video">Youtube video</param>
        /// <param name="postId">Reddit post id that the link was taken from</param>
        void videosInsertRequest_ResponseReceived(Video video, string postId)
        {
            mLog.Write(Log.LogLevel.Success, $"Video id {video.Id} was successfully uploaded!");

            /*Upload was successful, so now we'll find the reddit post that the video was 
            taken from and submit a comment containing the youtube link*/
            Post redditPost = null;
            for (int i = 0; i < 5; i++)
            {
                redditPost = mReddit.GetPost(new Uri($"https://www.reddit.com/{postId}/"));
                if (redditPost != null)
                {
                    redditPost.Comment(Functions.FormatComment(video.Id));
                    mLog.Write(Log.LogLevel.Success, $"Comment posted to thread {postId}");

                    break;
                }

                Thread.Sleep(500);
            }
        }


        /// <summary>
        /// Main worker completed
        /// </summary>
        private void MBackgroundWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                mLogError.Write(Log.LogLevel.Error, $"An unhandled exception caused mBackgroundWork to crash.\n{e.Error}");

            /*We don't really want to have the program exit by itself
            Something probably happened here*/
            mLog.Write(Log.LogLevel.Info, $"Worker has exited. Hmm.");
        }


        /// <summary>
        /// Authenticates our youtube account
        /// </summary>
        /// <returns>Returns task</returns>
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
