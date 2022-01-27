using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Hangfire;
using System.Linq;

namespace FfmpegPOC
{
    class Program
    {
        public static Process ffmpegProcess;
        static void Main(string[] args)
        {

            string wPath = @"C:\ffmpeg\records";
            string lPath = Path.Combine(Directory.GetCurrentDirectory(), "records");

            Console.WriteLine("VideoId giriniz");
            string videoId = Console.ReadLine();

            Console.WriteLine("IsLive Y/N");
            string isLive = Console.ReadLine();

            switch (isLive)
            {
                case "y":

                    LiveScenario(videoId, wPath, true);
                    break;

                default:
                    OfflineScenario(videoId);
                    break;
            }
            Console.ReadLine();
        }

        public static void OnExited(object sender,EventArgs e)
        {
            
        }
        public static void OnDisposed(object sender, EventArgs e)
        {

        }

        private static void LiveScenario(string videoId, string wPath, bool isVideoCut)
        {
            Console.WriteLine("Merhaba Live");

            string link = '"' + "https://www.youtube.com/watch?v=" + videoId + '"';
            string streamLinkCmd = "/C streamlink " + link + " --stream-url";
            string streamLink = string.Empty;
            DirectoryInfo directory;
            var path = wPath + "/" + videoId;
            
            Console.WriteLine(streamLinkCmd);
            var streamLinkStartInfo = new ProcessStartInfo
            {
                //FileName = "/usr/local/bin/streamlink",
                FileName = "cmd.exe",
                Arguments = streamLinkCmd,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var process = new Process { StartInfo = streamLinkStartInfo })
            {
                process.Start();
                StreamReader streamReader = process.StandardOutput;
                streamLink = streamReader.ReadToEnd();
                process.WaitForExit();
                process.Dispose();
            }

            string ffmpegCmd = "ffmpeg -y -i " + '"' + streamLink.TrimEnd() + '"' + " -hls_time 9 -hls_segment_filename " + '"' + "index-%d.ts" + '"' + " -hls_playlist_type vod index.m3u8";

            if (isVideoCut)
                ffmpegCmd += " -t 120";

            Console.WriteLine(ffmpegCmd);

            
            if (!Directory.Exists(path))
                directory = Directory.CreateDirectory(path);

            var ffmpegStartInfo = new ProcessStartInfo
            {
                //FileName = "/bin/bash",
                FileName = "cmd.exe",
                Arguments = @"/C " + ffmpegCmd,
                WorkingDirectory = path,
                UseShellExecute = false
            };

            try
            {
                using (ffmpegProcess = new Process { StartInfo = ffmpegStartInfo })
                {
                    ffmpegProcess.Exited += OnExited;
                    ffmpegProcess.Disposed += OnDisposed;
                    var firstDt = DateTime.Now;

                    ffmpegProcess.Start();
                    Console.WriteLine("ProcessorName: " + ffmpegProcess.ProcessName);


                    var timer = new System.Threading.Timer((e) =>
                    {
                        DirectoryInfo info = new DirectoryInfo(wPath);
                        FileInfo file = info.GetFiles().Where(x => x.FullName.Contains(".ts")).OrderByDescending(p => p.CreationTime).FirstOrDefault();
                        if (file != null)
                        {
                            if (file.CreationTime.AddMinutes(1) < DateTime.Now)
                            {
                                //Docker üzerinden container kaldırılacak.
                                Console.WriteLine("StartTime: " + ffmpegProcess.StartTime);
                                Console.WriteLine("ProcessorTime: " + ffmpegProcess.TotalProcessorTime.TotalMinutes);
                                ffmpegProcess.Kill(true);
                            }
                        }

                    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

                    ffmpegProcess.WaitForExit();


                    if (ffmpegProcess.HasExited)
                    {
                        Console.WriteLine("StartTime: " + ffmpegProcess.StartTime);
                        Console.WriteLine("ProcessorTime: " + ffmpegProcess.TotalProcessorTime.TotalMinutes);
                        ffmpegProcess.Kill(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Environment.Exit(0);
            }
        }
        // TODO:@ucaselimyavuz 
        private static void OfflineScenario(string videoId) { }
        private static void CheckLiveVideo()
        {

        }
    }
}




