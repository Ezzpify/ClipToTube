using System;

namespace ClipToTube
{
    class Program
    {
        /// <summary>
        /// Our main session
        /// </summary>
        private static Session mSession;


        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">No args</param>
        static void Main(string[] args)
        {
            Console.Title = "ClipToTube";
            var settings = Settings.GetSettings();

            if (settings != null)
                mSession = new Session(settings);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
