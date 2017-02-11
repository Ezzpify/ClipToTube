using System;
using System.Threading;
using System.Net;
using System.IO;

namespace ClipToTube
{
    class Web
    {
        /// <summary>
        /// Downloads a string from the internet from the specified url
        /// </summary>
        /// <param name="url">Url to download string from</param>
        /// <returns>Returns string of url website source</returns>
        public static string DownloadString(string url)
        {
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36");
                    return wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Message);
                    return string.Empty;
                }
            }
        }


        /// <summary>
        /// Downloads file
        /// </summary>
        /// <param name="url">Uurl to download from</param>
        /// <returns>Retuurns datetime now ticks as filename if successful</returns>
        public static string DownloadFile(string url)
        {
            using (WebClient wc = new WebClient())
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36");

                        string fileName = DateTime.Now.Ticks.ToString() + ".mp4";
                        string filePath = Path.Combine(Functions.GetAppFolder() + "Clips", fileName);

                        wc.DownloadFile(url, filePath);
                        Thread.Sleep(500);

                        if (File.Exists(filePath))
                            return filePath;

                        Thread.Sleep(1500);
                        break;
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine($"Weberror at DownloadFile {ex}");
                    }
                }
            }

            return string.Empty;
        }
    }
}
