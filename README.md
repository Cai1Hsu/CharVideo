This program allows you to play a video in chars.

The code is a little messy, I'll clean it another time.
## Features
   - High-performance   You can play videos at 60 fps in your terminal emulator.
   - Eazy to use        Follow the commands below and you can enjoy it easily.

## Get started

### 1. Make sure you have installed all the dependencies .

   - .Net 5
   - ffmpeg
   - libgdiplus



#### .Net 5

```bash
wget https://download.visualstudio.microsoft.com/download/pr/7f736160-9f34-4595-8d72-13630c437aef/b9c4513afb0f8872eb95793c70ac52f6/dotnet-sdk-5.0.102-linux-x64.tar.gz
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-5.0.102-linux-x64.tar.gz -C $HOME/dotnet
echo "\nexport PATH=$PATH:$HOME/dotnet\nexport DOTNET_ROOT=$HOME/dotnet">>~/.zshrc
soruce ~/.zshrc
```

   ##### Arch

   ```bash
sudo pacman -S ffmpeg libgdiplus git
   ```

   ##### Debian/Ubuntu

   ```bash
sudo apt install ffmpeg libgdiplus git
   ```

### 2. Build it.

   ```bash
git clone github.com/cai1xu/CharVideo
cd CharVideo
dotnet restore
dotnet build
   ```

### 3.  Install (optional) 

```bash
sudo ln -s $(pwd)/bin/Debug/net5.0/charvideo /usr/bin/charplayer
```



### 4. Enjoy

RECOMMEND : You should really try this super awesome terminal emulator [Alacritty](https://github.com/alacritty/alacritty).

```bash
charplayer [videofile] -f [fps] -r [resolution] -a
```

### On a new PC ? Get Started with the commands below at one step.
#### Arch
```bash
sudo apt install ffmpeg libgdiplus git
wget https://download.visualstudio.microsoft.com/download/pr/7f736160-9f34-4595-8d72-13630c437aef/b9c4513afb0f8872eb95793c70ac52f6/dotnet-sdk-5.0.102-linux-x64.tar.gz
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-5.0.102-linux-x64.tar.gz -C $HOME/dotnet
echo "\nexport PATH=$PATH:$HOME/dotnet\nexport DOTNET_ROOT=$HOME/dotnet">>~/.bashrc
soruce ~/.bashrc
git clone github.com/cai1xu/CharVideo
cd CharVideo
dotnet build
sudo ln -s $(pwd)/bin/Debug/net5.0/charvideo /usr/bin/charplayer
```

#### Debian

```Bash
sudo apt install ffmpeg libgdiplus git
wget https://download.visualstudio.microsoft.com/download/pr/7f736160-9f34-4595-8d72-13630c437aef/b9c4513afb0f8872eb95793c70ac52f6/dotnet-sdk-5.0.102-linux-x64.tar.gz
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-5.0.102-linux-x64.tar.gz -C $HOME/dotnet
echo "\nexport PATH=$PATH:$HOME/dotnet\nexport DOTNET_ROOT=$HOME/dotnet">>~/.bashrc
soruce ~/.bashrc
git clone github.com/cai1xu/CharVideo
cd CharVideo
dotnet build
sudo ln -s $(pwd)/bin/Debug/net5.0/charvideo /usr/bin/charplayer
```

## Usage

        -f						Specify the value of the fps

        -r						Specify the value of the resolution/ratio

        -a						Enable audio player

        --realtime		        Render frames while playing
        
        -c                      Play the video with colors
        
        -m                      Play the video as big as your terminal can
