This program allows you to play a video in chars.

## Get started

### 1. Make sure you have installed all the dependencies .

   - .Net 5
   - ffmpeg
   - libgdiplus

   ##### Arch

   ```bash
   $ sudo pacman -S ffmpeg libgdiplus
   ```

   ##### Debian/Ubuntu

   ```bash
   $ sudo apt install ffmpeg libgdiplus
   ```

### 2. Build it.

   ```bash
   $ git clone github.com/cai1xu/CharVideo
   $ cd CharVideo
   $ dotnet restore
   $ dotnet build
   ```

### 3.  Install (optional) 

```bash
   $ sudo ln -s ./CharVideo/bin/Debug/Net5.0/charvideo /usr/bin/charplayer
```



### 4. Enjoy

RECOMMEND : You should really try this super awesome terminal emulator [Alacritty](https://github.com/alacritty/alacritty).

```bash
   $ charplayer [videofile] -f [fps] -r [resolution] -a -e
```




