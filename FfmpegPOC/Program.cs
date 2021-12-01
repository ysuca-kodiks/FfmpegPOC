using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Hangfire;

namespace FfmpegPOC
{
    class Program
    {
        static void Main(string[] args)
        {

            string wPath = @"C:\ffmpeg\records";
            string lPath = Path.Combine(Directory.GetCurrentDirectory(),"records");

            Console.WriteLine("VideoId giriniz");
            string videoId = Console.ReadLine();

            Console.WriteLine("IsLive Y/N");
            string isLive = Console.ReadLine();



            switch (isLive)
            {
                case "y":
                    LiveScenario(videoId,wPath);
                    break;

                default:
                    OfflineScenario(videoId);
                    break;
            }


            Console.ReadLine();


        }

        private static void LiveScenario(string videoId,string wPath)
        {
            Console.WriteLine("Merhaba Live");

            string link = '"' + "https://www.youtube.com/watch?v=" + videoId + '"';
            string streamLinkCmd = "/C streamlink " + link + " --stream-url";
            string streamLink = string.Empty;

            Console.WriteLine(streamLinkCmd);
            var streamLinkStartInfo = new ProcessStartInfo
            {
                //FileName = "/usr/local/bin/streamlink",
                FileName="cmd.exe",
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

            string ffmpegCmd = "/C ffmpeg -y -i t=1" + '"' + streamLink.TrimEnd() + '"' + " -hls_time 9 -hls_segment_filename " + '"' + "index-%d.ts" + '"' + " -hls_playlist_type vod index.m3u8";
            Console.WriteLine(ffmpegCmd);

            var ffmpegStartInfo = new ProcessStartInfo
            {
                //FileName = "/bin/bash",
                FileName="cmd.exe",
                Arguments = ffmpegCmd,
                WorkingDirectory = wPath,
                UseShellExecute = false,
                

            };

            using (var ffmpegProcess = new Process { StartInfo = ffmpegStartInfo })
            {
                var firstDt = DateTime.Now;
                ffmpegProcess.Start();

                

                Console.WriteLine("ProcessorName: " + ffmpegProcess.ProcessName);
                //Task.Delay(TimeSpan.);

                //ffmpegProcess.Kill(true);

                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.HasExited)
                {
                    Console.WriteLine("StartTime: " + ffmpegProcess.StartTime);
                    Console.WriteLine("ProcessorTime: " + ffmpegProcess.TotalProcessorTime.TotalMinutes);
                    ffmpegProcess.Kill(true);
                }
            }


        }
        
        // TODO:@ucaselimyavuz 
        private static void OfflineScenario(string videoId) { }

    }




}




