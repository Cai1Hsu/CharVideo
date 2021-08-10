using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

int len = -1;
int fps = 30;
int videoWidth = 117;
int videoHeight = 33;
string ratio = "16:9";
string framesDir = null;
char[] tempString = null;
bool isRealtime         = true;
bool isWithColor        = false;
bool isPlayAudio        = true;
bool isOutputOnly       = false;
bool isFramesExist      = false;
bool isPlaySourceVideo  = false;
bool isGotFps           = false;
bool isInputRatio       = false;
bool isMaximize         = false;
const char escapeChar = (char)27;

void Main(string[] args)
{
    Console.CursorVisible = true;

    if (args.Length < 1 || args[0].ToLower() == "help" || args[0].ToLower() == "-h")
    {
        Console.WriteLine(@"Usage : CharVideo [videofile](path) [option]
eg : CharVideo ~/a.mp4
	option:
		--output_only 		-o 		Only output images and exit.
		-f 					-f 		Input fps manually.
		-r 					-r		Input resolution manually.
		-c 					-c		Use 256 colors.
		--pre-render 		-pr 	Render frames before play.
		-na 				-na		Do not play audio.");
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
                    Console.WriteLine("invalid input, fps was set to 30 by default.");
                }
                isGotFps = true;
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
                isMaximize = true;               
                break;
            case "--pre-render":
            case "-pr":
                isRealtime = false;
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
    
    if(!isGotFps){
        Console.Write("Getting video frames rate...\t");
        fps = GetVideoFps($"{video.FullName}");
        Console.WriteLine(fps);
    }

    if(!isInputRatio){
        Console.Write("Getting video ratio...\t");
        ratio = GetVideoRatio($"{video.FullName}");
        Console.WriteLine(ratio);
        if(ratio.Length == 0 || !ratio.Contains(":")){
            Console.WriteLine("Please input video ratio.");
            return;
        }
        isMaximize = true;
    }
    
    if(isMaximize){
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

    if(Directory.Exists($"{path}{name}_{fps}") && Directory.GetFiles($"{path}{name}_{fps}").Length > 0){
        isFramesExist = true;
    }else{
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
    }

    for (int i = 0; i++ < Console.WindowHeight; Console.Write('\n')) ;

    if(isRealtime) tempString = new char[isWithColor ? (videoWidth + 1) * videoHeight * 14 + 1: (videoWidth + 1) * videoHeight + 1];
    
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

    Play(isRealtime, isRealtime? null: frames, amont, fps, isRealtime? framesDir: null);
    Console.CursorVisible = true;
}

void Play(bool isRealtime, char[][] frames, int amont, int fps, string path)
{
	Stopwatch timer = new Stopwatch();
	timer.Reset();
    timer.Start();
    long playingFrame = 0;
    long lastSecond = timer.ElapsedMilliseconds / 1000;
    int countFrames = 1;
    int showingFps = fps;
    long lastFrame = 0;
    GetFrame(playingFrame);
    while (playingFrame < amont)
    {
        // print frame
        if(isRealtime){
            Console.Out.Write(tempString,0,len);
        }else Console.Write(frames[playingFrame]);
        if(isWithColor) Console.Write($"{escapeChar}[0m");
        Console.Write("{0} / {1} Rendering fps : {2}", playingFrame, amont, showingFps);
        
        lastFrame = playingFrame;
        // rendering fps counter
        if (timer.ElapsedMilliseconds / 1000 != lastSecond)
        {
            showingFps = countFrames;
            countFrames = 1;
            lastSecond = timer.ElapsedMilliseconds / 1000;
        }
        else countFrames++;
        // pre-render
        if(playingFrame != amont - 1) GetFrame(playingFrame + 1);
        // reset cursor
        Console.SetCursorPosition(0,0);
        // frame limit
        SpinWait.SpinUntil(() => (playingFrame = timer.ElapsedMilliseconds * fps / 1000) > lastFrame );
    }
    timer.Stop();
}

string GetPath(string name) => name.Substring(0, name.LastIndexOf(Path.DirectorySeparatorChar) + 1);

string StringToString(string str) => str.Length == 0?str:(str[0] == '\"' ? str : ('\"' + str + '\"') );

void PlaySource(string videoFile)
{
    string arg = string.Format("{0} -an -autoexit -loglevel quiet", StringToString(videoFile));
    Process.Start("ffplay", arg).WaitForExit();
}

void PlayAudio(string videoFile)
{
    string arg = string.Format("{0} -nodisp -autoexit -loglevel quiet", StringToString(videoFile));
	Process.Start("ffplay", arg).WaitForExit();
}

void OutputFrames(string pathandname, int fps, string path)
{
    string arg = string.Format(" -i {0} -r {1} -s {2}x{3} {4} -threads 4 -preset ultrafast -c:v h264_nvenc -cq 51 -loglevel quiet",	
		StringToString(pathandname), fps, videoWidth, videoHeight, StringToString($"{path}%d.png"));
	Process.Start("ffmpeg", arg).WaitForExit();
}

int GetVideoFps(string file){
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

string GetVideoRatio(string file){
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

void ProcessFrames(string path, int amont, ref char[][] frames)
{
    int length = isWithColor ? (videoWidth + -1) * videoHeight * 14 + 1: (videoWidth + 1) * videoHeight;
    for(int i = 0; i < amont;frames[i++] = new char[length]);
    for(int i = 0; i < amont;FrameToString(ref frames[i],new Bitmap($"{path}{++i}.png"))) ;
}

char[] GetFrame(long i){
    using (Bitmap bmp = new Bitmap($"{framesDir}{i + 1}.png"))
    {
        FrameToString(ref tempString ,bmp);
    }
    return isRealtime?null:tempString;
}

void FrameToString(ref char[] s, Bitmap bp)
{
    int i = 0;
    for (int y = 0; y < videoHeight; y++)
    {
        for (int x = 0; x < videoWidth; x++)
        {
            Color c = bp.GetPixel(x, y);
            if (isWithColor)
            {
                AppendChar(s, ref  i, escapeChar);
                AppendString(s, ref i, "[0;38;5;");
                AppendString(s, ref i, pixelToInt(bp.GetPixel(x, y)).ToString());
                AppendChar(s, ref i, 'm');
            }
            AppendChar(s, ref i, PixelToChar((c.R * 306 + c.G * 601 + c.B * 117) >> 10));
        }
        AppendChar(s, ref i, '\n');
    }
    len = i;
}

void AppendString(char[] str, ref int i,string s){
    for(int l = 0;l < s.Length;l++) str[i++] = s[l];
}

void AppendChar(char[] str, ref int i, char c) => str[i++] = c;

const string map = "              -----::::++++++=====*****###########";//"        --::+++++===***######";

char PixelToChar(int g) => map[g * 50 / 256];

int pixelToInt(Color c) => (c.R == c.G && c.G == c.B) ? 232 + (c.R * 23) / 255 : (16 + ((c.R * 5) / 255) * 36 + ((c.G * 5) / 255) * 6 + (c.B * 5) / 255);

void Cancled(object sender, ConsoleCancelEventArgs args){
	Console.CursorVisible = true;
    Console.Write($"{escapeChar}[0m");
}

Main(args);
