# Lively - Animated Wallpaper System
[![GitHub release](https://img.shields.io/github/release/rocksdanister/lively/all.svg)](https://github.com/rocksdanister/lively/releases)
[![Github all releases](https://img.shields.io/github/downloads/rocksdanister/lively/total.svg)](https://github.com/rocksdanister/lively/releases)

## Contents
- [About](#about)
- [Features](#features)
- [Download](#download)
- [Issues](#issues)
- [Contributing](#contributing)
- [Support](#support)
- [License](#license)

## About
![demo-gif](/resources/preview.gif?raw=true "demo")

(This is preview of upcoming v1.0 update)

Turn Video & GIF Files, Emulators, HTML, Web address & Shaders, Games into Windows desktop wallpaper; **Wallpapers will completely pause playback( 0% cpu & gpu usage) when fullscreen application/games are running.**

![demo-gif2](/resources/dragdrop.gif?raw=true "dragdrop")

Just drag & drop files, webpages to set as wallpaper..

#### Join Discussions:
* <a href="https://discord.gg/TwwtBCm">Discord group</a>
* <a href="https://www.reddit.com/r/LivelyWallpaper/">Reddit</a>

Lively is still in development, if you encounter bugs create a github Issue along with <a href="https://github.com/rocksdanister/lively/wiki/Common-Problems"> log file</a>

Help translate lively to other languages: <a href="https://github.com/rocksdanister/lively-translations">Translation Files</a>

<a href="https://github.com/rocksdanister/lively/wiki">Full Documentation</a>
## Features
*Wait a sec, preview gif clips take some time to load.*
#### Video
![demo-gif2](/resources/vid.gif?raw=true "video")

<a href="https://www.pexels.com/video/aerial-view-of-a-foggy-landscape-2547258/">Foggy Landscape</a> by Tom Fisk
* Use external codec packs or internal windows codec.
* Play .mp4, mkv, webm, avi, mov etc 
* Hardware Acceleration support.
* Audio will mute when not on desktop.
#### Youtube & streams
![demo-gif3](/resources/yt-gif.gif?raw=true "html")

* Just drag & drop youtube link to set as desktop wallpaper.
* Video quality is adjustable in settings.
* Additional software <a href="https://github.com/rocksdanister/lively/wiki/Youtube-Wallpaper">required.</a>
#### Web Pages & html
![demo-gif7](/resources/html.gif?raw=true "html")

<a href="http://louie.co.nz/25th_hour/"> 25th Hour</a> by Loius Coyle
* Load HTML file or web address as wallpaper.
* Runs webgl, javascript .. basically anything that works on chrome.
* Audio Reactive Wallpaper support, create wallpapers that react to <a href="https://github.com/rocksdanister/lively/wiki/Web-Guide-II-:-System-Audio-Data">system audio</a>
* Customisation support, <a href="https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction">documentation.</a>
#### Shaders
![demo-gif7](/resources/shadertoy.gif?raw=true "htmlshadertoy") 

<a href="https://www.shadertoy.com/view/lscczl">The Universe Within</a>, <a href="https://www.shadertoy.com/view/MdfBRX">The Drive Home</a> by BigWIngs
* Run GLSL shaders in browser.
* Shadertoy.com urls are supported as wallpaper.
#### Retro Game Emulators
![demo-gif4](/resources/emulator.gif?raw=true "html") 
* Coming soon
#### Games
![demo-gif5](/resources/unity.gif?raw=true "unity") 
* Can launch Unity & Godot games as wallpaper.
* Dynamic audio visualisers, 3D scenes..
#### GIFs
![demo-gif6](/resources/gif.gif?raw=true "gif")

<a href="https://giphy.com/gifs/nyan-cat-sIIhZliB2McAo"> Nyan cat</a>
* Make Memes/Cinemagraphs as wallpaper ... 
#### Other Applications
* Experimentail, works for some.
#### & more:
- Easy to use, Just drag & drop media files & webpages into lively window to set it as wallpaper.
- Real-time customisable web wallpaper support.
- Easy to share Lively-zip format, just drag & drop the zip file to import it into Library, use the built-in creator to make them (ensures compatibility).
- Hardware accelerated video playback, with option to use external Directshow codec of your choice. ( LAV(recommended), K-Lite, Kawaii Codec..)
- Library to browse and preview wallpapers.
- You can use it alongside rainmeter.
- Efficient, its a native c# wpf application with c++ calls. 
- Fully opensource & free; no blackmagic, no features behind paywall.
#### multiple monitor support:
- Full Multiple monitor support.
- Span single wallpaper across all screens.
- Duplicate same wallpaper all screens.
- Different wallpaper per screens.
#### Regarding Performance:
 * Wallpaper playback pauses when fullscreen application/games run on the machine (~0% cpu, gpu usage). 
 * Optionally you can set wallpaper to only play when on desktop.
 * Application rules: Set wallpaper playback rules based on running foreground application.(like always pause when photoshop is open etc)
 * Based on display(multiple monitor): Pause wallpaper playback per-display(depending on which monitor fullscreen apps/games run) or all display(s).
 * Kill wallpaper when fullscreen apps/games run(coming soon).
 * Mute audio when not on desktop (or optionally always mute).

**_I'm not officially affiliated with Unity technologies, godot, shadertoy;_**
## Download
##### Latest version: v0.9.6.0 (Windows 10, 8.1)[What's new?](https://github.com/rocksdanister/lively/releases/tag/v0.9.6.0)
- [`Download Lively Installer`][direct-full-win32]  
   _102MB, Web wallpaper support & some sample wallpapers included._
- [`Download Lively Portable`][direct-full-portable-win32]  
  _111MB, (No Installation & updater) Web wallpaper support & some sample wallpapers included._
  
**Portable build: Latest Visual C++ Redistributable is required: [vc_redist.x86.exe](https://aka.ms/vs/16/release/vc_redist.x86.exe)**
   
[direct-full-win32]: https://github.com/rocksdanister/lively/releases/download/v0.9.6.0/lively_setup_x86_full_v0960.exe

[direct-full-portable-win32]: https://github.com/rocksdanister/lively/releases/download/v0.9.6.0/lively_portable_x86_full_v0960.zip

**Installer will give Smartscreen warning, [discussion.](https://github.com/rocksdanister/lively/issues/9)**

Certain antivirus software heuristics algorithm will detect lively as a virus, this is a false positive
**lively is fully [opensource](https://en.wikipedia.org/wiki/Free_and_open-source_software), you are free to inspect the code.**

[Having trouble? ](https://github.com/rocksdanister/lively/wiki/Common-Problems)
## Issues
~~[TODO List](https://trello.com/b/rdFFxuMF/lively-wallpaper-system)~~

See github [issues.](https://github.com/rocksdanister/lively/issues)

## Contributing
Code contributions are welcome, check [guidelines](https://github.com/rocksdanister/lively/wiki) for making pull request.

Some Lively languages are machine translated,

Help translate lively to other languages: <a href="https://github.com/rocksdanister/lively-translations">Translation Files</a>

##### Related Projects
https://github.com/rocksdanister/lively-cef

https://github.com/rocksdanister/lively-gallery

## Support
You can always help development by buying me a cup of coffee(paypal):
[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/P5P1U8NQ)

## License
Lively v1.0 onwards is licensed under GPL-3.

Previous version is licensed under MS-PL, see v0.9.6.0 branch for working builds.
