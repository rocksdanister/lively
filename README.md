

# Lively - Animated Wallpaper System
[![GitHub release](https://img.shields.io/github/release/rocksdanister/lively/all.svg)](https://github.com/rocksdanister/lively/releases)
[![Github all releases](https://img.shields.io/github/downloads/rocksdanister/lively/total.svg)](https://github.com/rocksdanister/lively/releases)

## Contents

- [About](#about)
- [Download](#download)
- [Features](#features)
- [Issues](#issues)
- [Attribution](#attribution)


## About
![demo-gif](/resources/sea.gif?raw=true "demo")

Turn  Video & GIF Files, Retro Emulators, HTML, Web address & Shadertoy, Unity Games, Godot Games & more into Windows desktop wallpaper; Software will completely pause( 0% cpu & gpu usage) when fullscreen application/games are running.

VLC & Chromium browser is included, no need to setup or download additional software (except emulator)... works straight away.

Originally made this as a console application for personal use before working on my other projects "rePaper" & "GBWallpaper". Lively is still in early development.. do not download right now if you don't want to deal with crashes/bugs.

## Download
##### SOON

## Features

#### Video (libVLC)
![demo-gif2](/resources/sea_extended.gif?raw=true "vlc") 
* Play .mp4, mkv, webm, avi, mov etc 
* Hardware Acceleration support.
* Audio will mute when not on desktop.
#### Web Pages & html (CefSharp)
![demo-gif3](/resources/html.gif?raw=true "html") 
* Load HTML file or web address as wallpaper.
* Runs webgl, shaders, js .. basically anything that works on chrome.
* Basic mouse input.
#### Shadertoy
![demo-gif7](/resources/shadertoy.gif?raw=true "htmlshadertoy") 
* Just copy-paste shadertoy url such as: [https://www.shadertoy.com/view/wsl3WB](https://www.shadertoy.com/view/wsl3WB), Lively will handle the rest!
#### Retro Emulators (BizHawk)
![demo-gif4](/resources/emulator.gif?raw=true "html") 
* BizHawk: Multisystem emulator, supports many consoles: [https://github.com/TASVideos/BizHawk](https://github.com/TASVideos/BizHawk)
* Emulator will pause when not on desktop.
#### Unity & Godot Games
![demo-gif5](/resources/unity.gif?raw=true "unity") 
* Run Unity & Godot games as wallpaper.
* 3D audio visualiser etc.
#### GIF
![demo-gif6](/resources/gif.gif?raw=true "gif") 
* Make Memes wallpaper ... because why not!

### Other Applications
* in the works.

_I'm not officially affiliated with unity, godot, bizhawk, shadertoy. 
## Issues & roadmap
#### Priority:-
* Passing input to child window turned out to be not as simple as using SetForegroundWindow, currently bizhawk uses its own keyhook & I did simple left click on chromium. Complete mouse & keyboard(optional) will need to be implemented next.
* Unity games compiled with "run in background" disabled will pause since its not in focus, need to find a solution.
* I'm just loading a blank page in chromium to pause; have to decide whether to minimize or write a function to retrieve processid of cef to pause the thread.
* VLC stutters for higher resolution video(4k etc) on some low-end systems if the gpu downclocks too much(power saving). Turning off hw acceleration or setting lively to high performance power mode in gpu control panel fixes this.
* Multimonitor systems not tested.
* Currently I'm just minimizing browser window to pause rendering, works in my limited testing scenario.. if not good enough will have to get the pid of cefsharp & force pause.
* Currently just loading a fullscreen embedded instance of shadertoy link, there is room for many improvements.
#### Not so priority:-
* UI rework, ditch winform?
* Library & Playlist.
* Wallpaper creation tools.

## Attribution
to be filled

