

# Lively - Animated Wallpaper System
[![GitHub release](https://img.shields.io/github/release/rocksdanister/lively/all.svg)](https://github.com/rocksdanister/lively/releases)
[![Github all releases](https://img.shields.io/github/downloads/rocksdanister/lively/total.svg)](https://github.com/rocksdanister/lively/releases)

## Contents

- [About](#about)
- [Download](#download)
- [Features](#features)
- [Issues](#issues)
- [Attribution](#attribution)
- [License](#license)

## About
![demo-gif](/resources/sea.gif?raw=true "demo")

Turn Video & GIF Files, Emulators, HTML, Web address & Shaders, Games into Windows desktop wallpaper; Software will completely pause( 0% cpu & gpu usage) when fullscreen application/games are running.

Originally made this as a console application for personal use before working on my other projects "rePaper" & "GBWallpaper". Lively is still in early development.. do not download right now if you don't want to deal with crashes/bugs.

## Download
##### SOON

## Features

#### Video
![demo-gif2](/resources/sea_extended.gif?raw=true "vlc")

<a href="https://www.pexels.com/video/waves-crashing-to-the-shore-1536350/">Waves</a> by Tom Fisk
* LibVLC library.
* Play .mp4, mkv, webm, avi, mov etc 
* Hardware Acceleration support.
* Audio will mute when not on desktop.
#### Web Pages & html
![demo-gif3](/resources/html.gif?raw=true "html")

<a href="http://louie.co.nz/25th_hour/"> 25th Hour</a> by Loius Coyle
* CefSharp.
* Load HTML file or web address as wallpaper.
* Runs webgl, javascript .. basically anything that works on chrome.
* Basic mouse input.
#### Shaders
![demo-gif7](/resources/shadertoy.gif?raw=true "htmlshadertoy") 

<a href="https://www.shadertoy.com/view/wsl3WB">Hexagone</a> by BigWIngs
* Run GLSL shaders in browser.
* Shadertoy urls are supported in browser.
#### Emulators
![demo-gif4](/resources/emulator.gif?raw=true "html") 
* Emulator used currently is BizHawk, supports many retro systems: [https://github.com/TASVideos/BizHawk](https://github.com/TASVideos/BizHawk)
* Emulator will pause when not on desktop.
#### Games
![demo-gif5](/resources/unity.gif?raw=true "unity") 
* Can launch Unity & Godot games as wallpaper.
* Audio visualisers etc..
#### GIFs
![demo-gif6](/resources/gif.gif?raw=true "gif")

<a href="https://www.deviantart.com/maskman626/art/Young-ranger-706476994">Young ranger</a> by maskman626 
* Make Memes as wallpaper ... 

### Other Applications
* in the works.

**_I'm not officially affiliated with unity, godot, bizhawk, shadertoy;_**
## Issues & roadmap
#### Priority:-
* Passing input to child window turned out to be not as simple as using SetForegroundWindow, currently bizhawk uses its own keyhook & I did simple left click on chromium. Complete mouse & keyboard(optional) will need to be implemented next.
* Unity games compiled with "run in background" disabled will pause since its not in focus, need to find a solution.
* ~~I'm just loading a blank page in chromium to pause; have to decide whether to minimize or write a function to retrieve processid of cef to pause the thread.~~ Currently minimizing browser window to pause rendering, works in my limited testing scenario.. if not good enough will have to get the pid of cefsharp & force pause.
* VLC stutters for higher resolution video(4k etc) on some low-end systems if the gpu downclocks too much(power saving). Turning off hw acceleration or setting lively to high performance power mode in gpu control panel fixes this.
* Multimonitor systems not tested.
* Currently loading a fullscreen embedded instance of shadertoy links, there is room for much optimisation.
#### Not priority:-
* UI rework, ditch winform?
* Library & Playlist.
* Wallpaper creation tools.

## Attribution
todo

## License
todo

