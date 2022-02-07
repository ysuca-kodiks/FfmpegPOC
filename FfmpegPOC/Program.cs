using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Hangfire;
using System.Linq;
using System.Text;

namespace FfmpegPOC
{
    class Program
    {
        public static Process ffmpegProcess;
        public static string lPath = Path.Combine(Directory.GetCurrentDirectory(), "records");
        static void Main(string[] args)
        {
            Console.WriteLine("VideoId giriniz");
            string videoId = Console.ReadLine();

            Console.WriteLine("IsLive Y/N");
            string isLive = Console.ReadLine();
            if (!string.IsNullOrEmpty(videoId) && !string.IsNullOrEmpty(isLive))
                SetStartInfo(videoId, isLive);
            Console.ReadLine();
        }
        public static void SetStartInfo(string videoId, string isLive)
        {
            var path = lPath + "/" + videoId + "-" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            ProcessStartInfo _processStartInfo = new ProcessStartInfo();
            _processStartInfo.WorkingDirectory = path;
            _processStartInfo.FileName = (isLive == "y" ? "cmd.exe" : @"youtube-dl.exe");
            _processStartInfo.UseShellExecute = false;
            _processStartInfo.RedirectStandardOutput = true;
            switch (isLive)
            {
                case "y":
                    string link = '"' + "https://www.youtube.com/watch?v=" + videoId + '"';
                    _processStartInfo.Arguments = "/C streamlink " + link + " --default-stream 360p,240p,best --stream-url";
                    LiveScenario(_processStartInfo, true);
                    break;

                default:
                    _processStartInfo.RedirectStandardInput = true;
                    _processStartInfo.CreateNoWindow = true;
                    _processStartInfo.Arguments = @"-f bestvideo[height<=480,ext=mp4]+bestaudio[ext=m4a] http://www.youtube.com/watch?v=" + videoId;
                    _processStartInfo.RedirectStandardError = true;
                    OfflineScenario(_processStartInfo);
                    break;
            }
        }
        private static void LiveScenario(ProcessStartInfo startInfo, bool isVideoCut)
        {
            Console.WriteLine("Merhaba Live");
            string streamLink = string.Empty;
            using (var process = new Process { StartInfo = startInfo })
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
            startInfo.Arguments = @"/C " + ffmpegCmd;
            startInfo.UseShellExecute = false;
            try
            {
                using (ffmpegProcess = new Process { StartInfo = startInfo })
                {
                    var firstDt = DateTime.Now;
                    ffmpegProcess.Start();
                    var timer = new System.Threading.Timer((e) =>
                    {
                        DirectoryInfo info = new DirectoryInfo(lPath);
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
        private static void OfflineScenario(ProcessStartInfo processStartInfo)
        {
            Process myProcess = new Process();
            StringBuilder errorBuilder = new StringBuilder();

            myProcess.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                errorBuilder.Append(e.Data);
            };
            myProcess.StartInfo = processStartInfo;
            myProcess.Start();
            myProcess.BeginErrorReadLine();
            myProcess.WaitForExit();

        }
    }
}




