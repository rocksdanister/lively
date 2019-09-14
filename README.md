


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
![demo-gif](/resources/preview.gif?raw=true "demo")

Turn Video & GIF Files, Emulators, HTML, Web address & Shaders, Games into Windows desktop wallpaper; Software will completely pause( 0% cpu & gpu usage) when fullscreen application/games are running.

Originally made this as a console application for personal use before working on my other projects "rePaper" & "GBWallpaper". Lively is still in early development.
## Download
##### SOON

## Features

#### Video
![demo-gif2](/resources/sea_extended.gif?raw=true "vlc")

<a href="https://www.pexels.com/video/waves-crashing-to-the-shore-1536350/">Waves</a> by Tom Fisk
* Use external codec packs or internal windows codec.
* Play .mp4, mkv, webm, avi, mov etc 
* Hardware Acceleration support.
* Audio will mute when not on desktop.
#### Web Pages & html
![demo-gif3](/resources/html.gif?raw=true "html")

<a href="http://louie.co.nz/25th_hour/"> 25th Hour</a> by Loius Coyle
* Chromium Embedded Framework.
* Load HTML file or web address as wallpaper.
* Runs webgl, javascript .. basically anything that works on chrome.
* Audio Reactive Wallpaper support, create wallpapers that react system audio. 
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
* Audio visualisers, 3D wallpapers etc..
#### GIFs
![demo-gif6](/resources/gif.gif?raw=true "gif")

Nyan Cat
* Make Memes/Cinemagraphs as wallpaper ... 

### Other Applications
* in the works.

**_I'm not officially affiliated with unity, godot, bizhawk, shadertoy;_**
## Issues & roadmap
#### Priority:-
* Passing input to child window turned out to be not as simple as using SetForegroundWindow, currently bizhawk uses its own keyhook. Complete mouse & keyboard(optional) will need to be implemented next.
* Unity games compiled with "run in background" disabled will pause since its not in focus, need to find a solution.
* Tweaking Cefsharp audio reactive parameters.
* Multimonitor is currently limited to certain types of wallpapers, more types need to be supported in multiple display systems.
#### Not priority:-
* Wallpaper creation tools.

## Attribution
todo

## License
todo

