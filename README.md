![](https://github.com/Cai1Hsu/CharVideo/workflows/CodeQL/badge.svg)![](https://github.com/Cai1Hsu/CharVideo/workflows/.NET/badge.svg)


This program allows you to play a video in chars.

## Features
   - High-performance
   - Eazy to use
   - Cross platforms
   - 256 colors support
   - High fps

### Platforms
   - Linux
   - osx
   - Windows

## Binary
   For linux users, we provide [native-built binary](https://github.com/Cai1Hsu/CharVideo/releases/tag/v3.1).So you don't need a dotnet runtime.Install the dependencies and you can get started.
   - ffmpeg
   - ~~libgdiplus (Linux only)~~ no need anymore
   - glibc (Linux only)

## Build

### 1. Make sure you have installed all the dependencies .

   - .Net 7
   - ffmpeg
   
   ##### Arch

   ```bash
sudo pacman -S ffmpeg
   ```

   ##### Debian/Ubuntu

   ```bash
sudo apt install ffmpeg
   ```
   
   #### Install .Net7
   [.Net5](https://dotnet.microsoft.com/download/dotnet/7.0)
   
### 2. Build

   ```bash
git clone github.com/cai1hsu/CharVideo
cd CharVideo
dotnet restore
dotnet build
   ```

### 3.  Install (optional) 

```bash
sudo ln -s $(pwd)/bin/Debug/net5.0/charvideo /usr/bin/charplayer
```

### 4.

```bash
charplayer [videofile] [option]
```


## Usage
   `charvideo [videofile] [option]`
   
   eg : `charvideo a.mp4 --c`
   
#### Options
   - --f Input fps manually.
   - --r Input resolution manually.
   - --output_only -o Output Frames and exit.
   - --c Use 256 colors.
   - --pre-render -pr Render frames before play.
   - --na do not play audio
