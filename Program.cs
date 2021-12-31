using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging;

int len = -1;
int fps = 30;
int videoWidth = 117;
int videoHeight = 33;
string ratio = "16:9";
string framesDir = null;
char[] tempString = null;
bool isWithColor        = false;
bool isPlayAudio        = true;
bool isOutputOnly       = false;
bool isFramesExist      = false;
bool isPlaySourceVideo  = false;
bool isGotFps           = false;
bool isInputRatio       = false;
bool isMaximize         = false;
const char escapeChar   = (char)27;
const string map = "                ----::::++++++=====*****###########";//old : "        --::+++++===***######";

void Main(string[] args)
{
    Console.CursorVisible = true;

    if (args.Length < 1 || args[0].ToLower() == "help" || args[0].ToLower() == "-h")
    {
        Console.WriteLine(@"Usage : CharVideo [videofile](path) [option]
eg : CharVideo ~/a.mp4
    option:
        --output_only    -o     Only output images and exit.
        -f               -f     Input fps manually.
        -r               -r     Input resolution manually.
        -c               -c     Use 256 colors.
        --pre-render     -pr    Render frames before play.
        -na              -na    Do not play the audio.");
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
            case "-r":
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
            case "-f":
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
            case "-e":
                isFramesExist = true;
                break;
            case "-s":
                isPlaySourceVideo = true;
                break;
            case "-o":
            case "--output_only":
                isOutputOnly = true;
                break;
            case "-c":
                isWithColor = true;
                break;
            case "-m":
            case "--maximize":
                isMaximize = true;
                break;
            case "-na":
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
        Console.Write("Getting video fps...\t");
        fps = GetVideoFps($"{video.FullName}");
        Console.WriteLine(fps);
    }

    if(!isInputRatio)
    {
        Console.Write("Getting video ratio...\t");
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

    using(Bitmap bmp = new Bitmap($"{framesDir}{1}.png"))
    {
        tempString = new char[isWithColor ? (bmp.Width + 1) * bmp.Height * 14 + 1: (bmp.Width + 1) * bmp.Height + 1];
    }
    
    Console.CursorVisible = false;
    Console.CancelKeyPress += new ConsoleCancelEventHandler(Cancled);

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
    TextWriter buffer1 = new StringWriter();
    TextWriter buffer2 = new StringWriter();
    timer.Start();
    long playingFrame = 0;
    long lastSecond = timer.ElapsedMilliseconds / 1000;
    int countFrames = 1;
    int showingFps = fps;
    long lastFrame = 0;
    GetFrame(playingFrame);
    while (playingFrame < amont)
    {
        Console.SetOut(buffer1);
        
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
            buffer1.Write(tempString,0,len);
            if(isWithColor) buffer1.Write($"{escapeChar}[0m");
            buffer1.Write("{0} / {1} Rendering fps : {2}", playingFrame, amont, showingFps);
            TextWriter tempbuffer = buffer1;
            buffer1 = buffer2;
            buffer2 = tempbuffer;
            buffer1.Flush();
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
    string arg = string.Format(" -i {0} -r {1} -s {2}x{3} {4}  -preset ultrafast -loglevel -8",    
        StringToString(pathandname), fps, videoWidth, videoHeight, StringToString($"{path}%d.png"));
    Process.Start("ffmpeg", arg).WaitForExit();
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
    string arg = string.Format("-v quiet -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=display_aspect_ratio {0}", StringToString(file));
    Process p = new Process();
    p.StartInfo.FileName = "ffprobe";
    p.StartInfo.Arguments = arg;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.UseShellExecute = false;
    p.Start();
    string o = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return o;
}

void GetFrame(long i)
{
    using (Bitmap bmp = new Bitmap($"{framesDir}{i + 1}.png"))
    {
        FrameToString(ref tempString ,bmp);
    }
}

unsafe void FrameToString(ref char[] s, Bitmap bp)
{
    int i = 0;
    int lastColor = -1;
    BitmapData data = bp.LockBits(new Rectangle(0, 0, bp.Width, bp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    IntPtr ptr = data.Scan0;
    for (int y = 0; y < data.Height; y++)
    {
        for (int x = 0; x < data.Width; x++)
        {
            byte* pixelptr = (byte*)(ptr + (y * data.Stride + x * 3));
            if (isWithColor)
            {
                int currentColor = pixelToInt( pixelptr[2], pixelptr[1], pixelptr[0]); 
                if (lastColor != currentColor)
                {
                    AppendChar(s, ref  i, escapeChar);
                    AppendString(s, ref i, "[0;38;5;");
                    AppendString(s, ref i, currentColor.ToString());
                    AppendChar(s, ref i, 'm');
                    lastColor = currentColor;
                }
            }
            AppendChar(s, ref i, PixelToChar((pixelptr[2] * 15 + pixelptr[1] * 30 + pixelptr[0] * 6) >> 8));
        }
        AppendChar(s, ref i, '\n');
    }
    len = i;
    bp.UnlockBits(data);
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
