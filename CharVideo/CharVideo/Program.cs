using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Media;

namespace CharVideo
{
    class Program
    {
        public static int width = 128;
        public static int height = 37;
        public static int begin = 0;
        public static string audiofile;

        private static void Main(string[] args)
        {
            Console.CursorVisible = true;

            string rate = "16:9";
            int fps = 30;
            bool withaudio = false;
            bool framesexist = false;

            //--------------------Arguements processing begin---------------------------
            if (args.Length < 1 || args[0].ToLower() == "help")
            {
                Console.WriteLine(@"
Usage : CharVideo [videofile](absoluted path) -f [fps] -r [width:hight or width x height] (audio=true) (framesexist)
example CharVideo ~/a.mp4 -f 60 -r 4:3 audio=true framesexist");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File doesn't exists.");
                return;
            }

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-r":
                        if (args[++i].Contains(":"))
                        {
                            rate = args[i];
                            string[] a = args[i].Split(':');
                            if (args[i] == "16:9")
                            {
                                width = 117;
                                height = 33;
                            }
                        }
                        else
                        {
                            string[] size = args[i].Split('x');
                            width = Convert.ToInt32(size[0]);
                            height = Convert.ToInt32(size[1]);
                        }

                        break;
                    case "audio=true":
                        withaudio = true;
                        break;
                    case "-f":
                        try
                        {
                            fps = Convert.ToInt32(args[++i]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine("Fps was set to 30 by default.");
                        }

                        break;
                    case "framesexist":
                        framesexist = true;
                        break;
                    case "-b":
                    case "--begin":
                        try
                        {
                            begin = Convert.ToInt32(args[++i]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }

                        break;
                    case "--maximize":
                        var x = Convert.ToInt32(rate[0]);
                        var y = Convert.ToInt32(rate[1]);
                        var tempwidth = Convert.ToInt32(Math.Floor(Console.WindowHeight * x / y * 1.0f));
                        var tempheight = Convert.ToInt32(Math.Floor(Console.WindowWidth * y / x * 1.0f));
                        if (tempwidth > Console.WindowWidth)
                        {
                            width = Console.WindowWidth;
                            height = tempheight;
                        }
                        else
                        {
                            width = tempwidth;
                            height = Console.WindowHeight;
                        }
                        break;
                }
            }

            //--------------------Arguements processing end---------------------------

            FileInfo video = new FileInfo(args[0]);

            string path = GetPath(video.FullName);
            audiofile = video.FullName;

            if (!framesexist)
            {
                Directory.CreateDirectory(path + "/frames");
                OutputFrames(video.FullName, fps, path + "frames/");
            }
            
            int amontOfFrames = Directory.GetFiles(path + "/frames").Length;

            if (amontOfFrames == 0) return;

            string[] frames = new string[amontOfFrames];

            Console.WriteLine("I`m here 0 ");
            ProcessFrames(path, amontOfFrames, ref frames);

            Console.WriteLine("I`m here");
            Thread audioplayer = null;
            if (withaudio)
            {
                audioplayer = new Thread(() => {PlayAudio(video.FullName);});
            }

            Console.WriteLine("I`m here2");

            Console.Write("\n\aReady,press any key to continue.");
            Console.ReadKey(true);
            Console.Clear();

            Console.CursorVisible = false;

            for (int i = 3; i != 0; i--)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(i);
                Thread.Sleep(1000);
            }

            Console.Clear();

            Thread.Sleep(1000);
            Console.Clear();
            if (withaudio)
            {
                audioplayer.Start();
            }

            Play(ref frames, amontOfFrames, fps);

            Console.CursorVisible = true;
            return;
        }

        private static void Play(ref string[] frames, int amont, int fps)
        {
            long nowframe = 0;
            long starttime = DateTime.Now.Ticks;
            while (nowframe < amont)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(frames[nowframe]);
                Console.Write("Frame : {0} of {1} fps : {2}", nowframe, amont, fps);
                nowframe = (DateTime.Now.Ticks - starttime) * fps / 10000000 + begin;
            }
        }

        private static void PlayAudio(string videoFile)
        {
            string args = string.Format("{0} -nodisp -autoexit -loglevel quiet", audiofile);
            ProcessStartInfo p = new ProcessStartInfo("ffplay", args);
            p.CreateNoWindow = true;
            p.WindowStyle = ProcessWindowStyle.Hidden;
            p.RedirectStandardInput = false;
            p.RedirectStandardOutput = false;
            p.RedirectStandardError = false;
            p.UseShellExecute = false;
            Process VideoToFrames = new Process();
            VideoToFrames.StartInfo = p;
            VideoToFrames.Start();
            VideoToFrames.WaitForExit();
        }

        private static void OutputFrames(string pathandname, int fps, string path)
        {
            string args = string.Format(" -i \"{0}\" -r {1} -s {2}x{3} {4}%d.png", pathandname, fps, width, height,
                path);
            ProcessStartInfo p = new ProcessStartInfo("ffmpeg", args);
            p.CreateNoWindow = true;
            p.WindowStyle = ProcessWindowStyle.Hidden;
            Process VideoToFrames = new Process();
            VideoToFrames.StartInfo = p;
            VideoToFrames.Start();
            VideoToFrames.WaitForExit();
        }

        private static string GetPath(string name)
        {
            int t = name.LastIndexOf('/') + 1;
            return name.Substring(0, t);
        }

        private static void ProcessFrames(string path, int amont, ref string[] frames)
        {
            Console.WriteLine("amont = {0}",amont);
            for (int i = 1; i < amont; i++)
            {
                Bitmap bmp = new Bitmap(path + "frames/" + i.ToString() + ".png");
                frames[i - 1] = FrameToString(bmp);
                // Console.WriteLine(i);
            }
        }

        private static string FrameToString(Bitmap bp)
        {
            StringBuilder sb = new StringBuilder();
            for (int w = 0; w < height; w++)
            {
                for (int h = 0; h < width; h++)
                {
                    Color c = bp.GetPixel(h, w);
                    sb.Append(PixelToChar(c));
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static char PixelToChar(Color c)
        {
            byte g = (byte) ((c.R * 306 + c.G * 601 + c.B * 117) >> 10);
            if (g < 80) return ' ';
            if (g >= 75 && g < 100) return '-';
            if (g >= 100 && g < 120) return ':';
            if (g >= 120 && g < 150) return '+';
            if (g >= 150 && g < 175) return '=';
            if (g >= 175 && g < 200) return '*';
            return '#';
        }
    }
}
