using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Video;
using osu.Framework.Graphics.Audio;

int len = -1;
int fps = 30;
int videoWidth = 117;
int videoHeight = 33;
string ratio = "16:9";
string framesDir = null;
char[] BufferString = null;
bool isWithColor        = false;
bool isPlayAudio        = true;
bool isOutputOnly       = false;
bool isFramesExist      = false;
bool isPlaySourceVideo  = false;
bool isGotFps           = false;
bool isInputRatio       = false;
bool isMaximize         = false;
bool isWindows          = Environment.OSVersion.Platform == PlatformID.Win32NT;
const char escapeChar   = (char)27;
const string map = "                ----::::++++++=====*****###########";//old : "        --::+++++===***######";

IRenderer Renderer = new GLRenderer();
VideoDecoder videoDecoder;

void Main(string[] args)
{    
    Console.CursorVisible = true;

    if (args.Length < 1 || args[0].ToLower() == "help" || args[0].ToLower() == "-h")
    {
        Console.WriteLine(@"Usage : CharVideo [videofile](path) [option]
eg : CharVideo ~/a.mp4
    option:
        --output_only   Only output images and exit.
        --f             Input fps manually.
        --r             Input resolution manually.
        --c             Use 256 colors.
        --pre-render    Render frames before play.
        -na             Do not play the audio.");
        return;
    }

    if (!File.Exists(args[0]))
    {
        Console.WriteLine("File doesn't exists.");
        Console.WriteLine($"Inputed arg[0] : {args[0]}");
        return;
    }

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i].ToLower())
        {
            case "--r":
                if (args[++i].Contains(":"))
                {
                    ratio = args[i];
                    if (args[i] == "16:9")
                    {
                        videoWidth = 117;
                        videoHeight = 33;
                    }
                    if (args[i] == "4:3")
                    {
                        videoWidth = 88;
                        videoHeight = 33;
                    }
                }
                else
                {
                    string[] size = args[i].Split('x');
                    videoWidth = Convert.ToInt32(size[0]);
                    videoHeight = Convert.ToInt32(size[1]);
                }
                isInputRatio = true;
                break;
            case "--f":
                try
                {
                    fps = Convert.ToInt32(args[++i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Invalid input, fps was set to 30 by default.");
                }
                isGotFps = true;
                break;
            case "--e":
                isFramesExist = true;
                break;
            case "--s":
                isPlaySourceVideo = true;
                break;
            case "--o":
            case "--output_only":
                isOutputOnly = true;
                break;
            case "--c":
                isWithColor = true;
                break;
            case "--m":
            case "--maximize":
                isMaximize = true;
                break;
            case "--na":
                isPlayAudio = false;
                break;
            default:
                Console.WriteLine($"\a[!] Unexcptation argument : {args[i]}");
                return;
        }
    }

    FileInfo video = new FileInfo(args[0]);

    string name = video.Name.Substring(0, video.Name.LastIndexOf('.'));
    string path = GetPath(video.FullName);
    
    if(!isGotFps)
    {
        Console.Write("Fetching video fps...\t");
        fps = GetVideoFps($"{video.FullName}");
        Console.WriteLine(fps);
    }

    if(!isInputRatio)
    {
        Console.Write("Fetching video ratio...\t");
        ratio = GetVideoRatio($"{video.FullName}");
        Console.WriteLine(ratio);
        if(ratio.Length == 0 || !ratio.Contains(":"))
        {
            Console.WriteLine("Please input video ratio.");
            return;
        }
        isMaximize = true;
    }
    
    if(isMaximize)
    {
        int x = Convert.ToInt32(ratio.Split(':')[0]);
        int y = Convert.ToInt32(ratio.Split(':')[1]);
        int w1 = Console.WindowWidth;
        int h1 = (w1 * y / x) >> 1 - 1;
        int h2 = Console.WindowHeight - 1;
        int w2 = h2 * 2 * x / y;
        if (h1 > Console.WindowHeight)
        {
            videoHeight = h2;
            videoWidth = w2;
        }
        else
        {
            videoHeight = h1;
            videoWidth = w1;
        }
    }

    if(Directory.Exists($"{path}{name}_{fps}") && Directory.GetFiles($"{path}{name}_{fps}").Length > 0)
    {
        isFramesExist = true;
    }else
    {
        isFramesExist = false;
    }
    
    framesDir = $"{path}{name}_{fps}{Path.DirectorySeparatorChar}";

    if (!isFramesExist || !Directory.Exists(framesDir))
    {
        Console.Write("Processing frames.");
        Directory.CreateDirectory(framesDir);
        OutputFrames(video.FullName, fps, framesDir);
    }

    if (isOutputOnly)
    {
        Console.WriteLine("Done");
        return;
    }

    int amont = Directory.GetFiles(framesDir).Length;

    if (amont == 0) return;

    for (int i = 0; i++ < Console.WindowHeight; Console.Write('\n')) ;

    using(Image<Rgba32> image = Image.Load<Rgba32>($"{framesDir}{1}.png"))
    {
        BufferString = new char[isWithColor ? (image.Width + 1) * image.Height * 14 + 1: (image.Width + 1) * image.Height + 1];
    }
    
    Console.CancelKeyPress += new ConsoleCancelEventHandler(Cancled);
    Console.CursorVisible = false;

    if (isPlayAudio)
    {
        PlayAudio(video.FullName);
    }
    if (isPlaySourceVideo)
    {
        PlaySource(video.FullName);
    }

    Play(amont, fps, framesDir);
    Console.CursorVisible = true;
}

void Play(int amont, int fps, string path)
{
    Stopwatch timer = new Stopwatch();
    timer.Start();
    long playingFrame = 0;
    long lastSecond = timer.ElapsedMilliseconds / 1000;
    int countFrames = 1;
    int showingFps = fps;
    long lastFrame = 0;
    GetFrame(playingFrame);
    while (playingFrame < amont)
    {
        lastFrame = playingFrame;
     
        if (timer.ElapsedMilliseconds / 1000 == lastSecond)
        {
            countFrames++;
        }
        else
        {
            showingFps = countFrames;
            countFrames = 1;
            lastSecond = timer.ElapsedMilliseconds / 1000;
        }
     
        if(playingFrame != amont - 1)
        {
            GetFrame(playingFrame + 1);
            Console.Out.Write(BufferString,0,len);
            if(isWithColor) Console.Out.Write($"{escapeChar}[0m");
            Console.Out.Write("{0} / {1} Rendering fps : {2}", playingFrame, amont, showingFps);
            if (isWindows)
                Console.SetCursorPosition(0, 0);
            else
                Console.Out.Write("{0}[0;0H", escapeChar);
        }
     
        SpinWait.SpinUntil(() => (playingFrame = timer.ElapsedMilliseconds * fps / 1000) > lastFrame);
    }
    timer.Stop();
}

string GetPath(string name) => name.Substring(0, name.LastIndexOf(Path.DirectorySeparatorChar) + 1);

string StringToString(string str) => str.Length == 0?str:(str[0] == '\"' ? str : ('\"' + str + '\"') );

void PlaySource(string videoFile)
{
    string arg = string.Format("{0} -an -autoexit -loglevel -8", StringToString(videoFile));
    Process.Start("ffplay", arg);
}

void PlayAudio(string videoFile)
{
    string arg = string.Format("{0} -nodisp -autoexit -loglevel -8", StringToString(videoFile));
    Process.Start("ffplay", arg);
}

void OutputFrames(string pathandname, int fps, string path)
{
    VideoDecoder videoDecoder = new VideoDecoder(null, path);
    var decodedFrames = videoDecoder.GetDecodedFrames();
    
}

int GetVideoFps(string file)
{
    string arg = string.Format("-v quiet -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate {0}", StringToString(file)); 
    Process p = new Process();
    p.StartInfo.FileName = "ffprobe";
    p.StartInfo.Arguments = arg;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.UseShellExecute = false;
    p.Start();
    string o = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return Convert.ToInt32(o.Substring(0, o.LastIndexOf('/')));
}

string GetVideoRatio(string file)
{
    string arg = string.Format("-v quiet -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=width,height {0}", StringToString(file));
    Process p = new Process();
    p.StartInfo.FileName = "ffprobe";
    p.StartInfo.Arguments = arg;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.UseShellExecute = false;
    p.Start();
    string o = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return ResolutionToRatio(o);
}

string ResolutionToRatio(string input)
{
    string[] Resolution = input.Split('\n');
    int width = 0, height = 0;
    
    try 
    {
        width = Convert.ToInt32(Resolution[0]);
        height = Convert.ToInt32(Resolution[1]);
    } 
    catch (Exception e) 
    {
        Console.WriteLine(e.Message);
    }

    int gcdi = gcd(width, height);
    
    width = (int)(width / gcdi);
    height = (int)(height / gcdi);

    return $"{width}:{height}";
}

int gcd(int a, int b)
{
    int r;

    while (b != 0 )
    {
        r = a % b;
        a = b;
        b = r;
    }
      
    return a;
}

void GetFrame(long index)
{
    using (Image<Rgba32> image = Image.Load<Rgba32>($"{framesDir}{index + 1}.png"))
    {
        int i = 0;
        image.ProcessPixelRows(pixelAccessor =>
        {
            int lastColor = -1;
            for (int y = 0; y < pixelAccessor.Height; y++)
            {
                Span<Rgba32> row = pixelAccessor.GetRowSpan(y);

                // Using row.Length helps JIT to eliminate bounds checks when accessing row[x].
                for (int x = 0; x < row.Length; x++)
                {
                    Rgba32 pixel = row[x];
                    if (isWithColor)
                    {
                        int currentColor = pixelToInt( pixel.R, pixel.G, pixel.B); 
                        if (lastColor != currentColor)
                        {
                            AppendChar(BufferString, ref  i, escapeChar);
                            AppendString(BufferString, ref i, "[0;38;5;");
                            AppendString(BufferString, ref i, currentColor.ToString());
                            AppendChar(BufferString, ref i, 'm');
                            lastColor = currentColor;
                        }
                    }
                    AppendChar(BufferString, ref i, PixelToChar((pixel.R * 15 + pixel.G * 30 + pixel.B * 6) >> 8));
                }
                AppendChar(BufferString, ref i, '\n');
            }
            len = i;
        });
    }
}

void AppendString(char[] str, ref int i,string s)
{
    for(int l = 0;l < s.Length;l++) str[i++] = s[l];
}

void AppendChar(char[] str, ref int i, char c) => str[i++] = c;

char PixelToChar(int g) => map[g];

int pixelToInt(int r, int g, int b) => (g == r && g == b) ? 232 + (g * 23) / 255 : (16 + ((r * 5) / 255) * 36 + ((g * 5) / 255) * 6 + (b * 5) / 255);

void Cancled(object sender, ConsoleCancelEventArgs args)
{
    Console.CursorVisible = true;
    Console.Write($"{escapeChar}[0m");
}

Main(args);
