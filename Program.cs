using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

int fps = 30;
int videoWidth = 117;
int videoHeight = 33;      // Since a char includes 2 pixels, height should be the half of the source.
string ratio = "16:9";
bool isRealtime         = false;
bool isWithColor        = false;
bool isPlayAudio        = false;
bool isOutputOnly       = false;
bool isFramesExist      = false;
bool isPlaySourceVideo  = false;

void Main(string[] args)
{
    Console.CursorVisible = true;

    if (args.Length < 1 || args[0].ToLower() == "help" || args[0].ToLower() == "-h")
    {
        Console.WriteLine(@"
    Usage : CharVideo [videofile](absoluted path) -f [fps] -r [width:hight or width x height] -a(optional, means that you want to play audio) -e(optional, if there are frame files exist)
    example CharVideo ~/a.mp4 -f 60 -r 4:3 -a -e");
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
                break;
            case "-a":
                isPlayAudio = true;
                break;
            case "-f":
                try
                {
                    fps = Convert.ToInt32(args[++i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("invalid input, fps was set to 30 by default.");
                }
                break;
            case "-e":
                isFramesExist = true;
                break;
            case "-s":
                isPlaySourceVideo = true;
                break;
            case "--realtime":
                isRealtime = true;
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
                int x = Convert.ToInt32(ratio.Split(':')[0]);
                int y = Convert.ToInt32(ratio.Split(':')[1]);
                int w1 = Console.WindowWidth;
                int h1 = (w1 * y / x) >> 1;
                int h2 = Console.WindowHeight;
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
                break;
        }
    }

    FileInfo video = new FileInfo(args[0]);

    string name = video.Name.Substring(0, video.Name.LastIndexOf('.'));
    string path = GetPath(video.FullName);
    string framesDir = $"{path}{name}_{fps}/";

    if (!isFramesExist || !Directory.Exists(framesDir))
    {
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

    char[][] frames = new char[amont][];

    Thread audioPlayer = null;
    if (isPlayAudio)
    {
        audioPlayer = new Thread(() => { PlayAudio(video.FullName); });
    }
    Thread sourcePlayer = null;
    if (isPlaySourceVideo)
    {
        sourcePlayer = new Thread(() => { PlaySource(video.FullName); });
    }

    if (!isRealtime)
    {
        ProcessFrames(framesDir, amont, ref frames);

        Console.WriteLine("Please pesize your terminal emulator to {0}x{1}", videoWidth, videoHeight + 1);
        Console.Write("\n\aReady,press any key to continue.");
        Console.ReadKey(true);
    }
    Console.Clear();

    Console.CursorVisible = false;
    Console.CancelKeyPress += new ConsoleCancelEventHandler(Cancled);

    if (isPlayAudio)
    {
        audioPlayer.Start();
    }
    if (isPlaySourceVideo)
    {
        sourcePlayer.Start();
    }
    if (isRealtime) PlayRealtime($"{path}{name}_{fps}/", (long)amont, fps);
    else Play(ref frames, amont, fps);

    Console.CursorVisible = true;
}

void Play(ref char[][] frames, int amont, int fps)
{
    long playingFrame = 0;
    long startTime = DateTime.Now.Ticks;
    long lastSecond = startTime / 10000000;
    int countFrames = 1;
    int showingFps = fps;
    long lastFrame = 0;
    while (playingFrame < amont)
    {
        Console.Write(frames[playingFrame]);
        Console.Write("{3}[m {0}/{1} Rendering fps : {2} ", playingFrame, amont, showingFps, (char)27);
        long thisTick = DateTime.Now.Ticks;
        if (thisTick / 10000000 != lastSecond)
        {
            showingFps = countFrames;
            countFrames = 1;
            lastSecond = thisTick / 10000000;
        }
        else countFrames++;
        do
            playingFrame = (DateTime.Now.Ticks - startTime) * fps / 10000000;
        while (playingFrame == lastFrame);
        lastFrame = playingFrame;
        Console.SetCursorPosition(0, 0);
    }
}

void PlayRealtime(string path, long amont, int fps)
{
    long playingFrame = 0;
    long startTime = DateTime.Now.Ticks;
    long lastSecond = startTime / 10000000;
    int countFrames = 1;
    int showingFps = fps;
    long lastFrame = 0;
    while (playingFrame < amont)
    {
        Console.Write(GetFrame(playingFrame, path));
        Console.Write("{3}[m {0}/{1} Rendering fps[Realtime]: {2} ", playingFrame, amont, showingFps, (char)27);
        long thisTick = DateTime.Now.Ticks;
        if (thisTick / 10000000 != lastSecond)
        {
            showingFps = countFrames;
            countFrames = 1;
            lastSecond = thisTick / 10000000;
        }
        else countFrames++;
        do
            playingFrame = (DateTime.Now.Ticks - startTime) * fps / 10000000;
        while (playingFrame == lastFrame);
        lastFrame = playingFrame;
        Console.SetCursorPosition(0, 0);
    }
}

void PlaySource(string videoFile)
{
    string arg = string.Format("{0} -an -autoexit -loglevel quiet", videoFile);
    Process.Start("ffplay", arg);
}

void PlayAudio(string videoFile)
{
    string arg = string.Format("{0} -nodisp -autoexit -loglevel quiet", videoFile);
    Process.Start("ffplay", arg).WaitForExit();
}

void OutputFrames(string pathandname, int fps, string path)
{
    string arg = string.Format(" -i \"{0}\" -r {1} -s {2}x{3} {4}%d.png -loglevel quiet",
        pathandname, fps, videoWidth, videoHeight, path);
    Process.Start("ffmpeg", arg);
}

string GetPath(string name)
{
    return name.Substring(0, name.LastIndexOf('/') + 1);
}

void ProcessFrames(string path, int amont, ref char[][] frames)
{
    for (int i = 0; i < amont;)
    {
        frames[i - 1] = FrameToString(new Bitmap($"{path}{++i}.png"));
    }
}

char[] GetFrame(long i, string path)
{
    return FrameToString(new Bitmap($"{path}{i + 1}.png"));
}

const char slashE = (char)27;
const char end = (char)0;

char[] FrameToString(Bitmap bp)
{
    char[] s = new char[isWithColor ? (videoWidth + 1) * videoHeight * 14 + 1: (videoWidth + 1) * videoHeight + 1]; 
    int i = 0;
    for (int y = 0; y < videoHeight; y++)
    {
        for (int x = 0; x < videoWidth; x++)
        {
            Color c = bp.GetPixel(x, y);
            if (isWithColor)
            {
                AppendChar(ref s, ref i, slashE);
                AppendString(ref s, ref i, "[0;38;5;");
                AppendString(ref s, ref i, pixelToInt(bp.GetPixel(x, y)).ToString());
                AppendChar(ref s, ref i, 'm');
            }
            AppendChar(ref s, ref i, PixelToChar(c));
        }
        AppendChar(ref s, ref i, '\n');
    }
    AppendChar(ref s, ref i, end);
    return s;
}

void AppendString(ref char[] str, ref int i,string s){
    for(int l = 0;l < s.Length;l++) str[i++] = s[l];
}

void AppendChar(ref char[] str, ref int i, char c) => str[i++] = c;

char PixelToChar(Color c)
{
    int g = ((c.R << 1) + (c.G * 5) + c.B) >> 3;
    if (g < 80) return ' ';
    if (g >= 75 && g < 100) return '-';
    if (g >= 100 && g < 120) return ':';
    if (g >= 120 && g < 150) return '+';
    if (g >= 150 && g < 175) return '=';
    if (g >= 175 && g < 200) return '*';
    return '#';
}

int pixelToInt(Color c)
{
    if (c.R == c.G && c.G == c.B) return 232 + (c.R * 23) / 255;
    else return (16 + ((c.R * 5) / 255) * 36 + ((c.G * 5) / 255) * 6 + (c.B * 5) / 255);
}

void Cancled(object sender, ConsoleCancelEventArgs args) => Console.CursorVisible = true;

Main(args);
