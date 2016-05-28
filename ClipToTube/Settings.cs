using System;
using System.IO;
using Newtonsoft.Json;

namespace ClipToTube
{
    class Settings
    {
        /// <summary>
        /// Returns AccountSettings class if file exist, else it will spawn new settings file
        /// </summary>
        /// <returns>Returns null if no file exist</returns>
        public static Config.Settings GetSettings()
        {
            CreateAssetFolders();
            if (File.Exists(Endpoints.SETTINGS_FILE))
            {
                return JsonConvert.DeserializeObject<Config.Settings>
                    (File.ReadAllText(Endpoints.SETTINGS_FILE));
            }

            File.WriteAllText(Endpoints.SETTINGS_FILE,
                JsonConvert.SerializeObject(new Config.Settings(), Formatting.Indented));
            Console.WriteLine($"Settings file has been written at {Endpoints.SETTINGS_FILE}");

            return null;
        }


        /// <summary>
        /// Creates folders for our program
        /// </summary>
        private static void CreateAssetFolders()
        {
            Directory.CreateDirectory(Functions.GetAppFolder() + "Settings");
            Directory.CreateDirectory(Functions.GetAppFolder() + "Logs");
            Directory.CreateDirectory(Functions.GetAppFolder() + "Logs\\ErrorLogs");
            Directory.CreateDirectory(Functions.GetAppFolder() + "Clips");
        }
    }
}
