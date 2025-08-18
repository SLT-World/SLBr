<div align="center">
  
  # SLBr
  
  **A lightweight browser for a faster web**<br/>
  **Fast, lightweight browsing with a clean interface.**

[![C#](https://img.shields.io/static/v1?style=for-the-badge&message=C%23&color=239120&logo=csharp&logoColor=239120&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![XAML](https://img.shields.io/static/v1?style=for-the-badge&message=XAML&color=0C54C2&logo=XAML&logoColor=0C54C2&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![.NET](https://img.shields.io/static/v1?style=for-the-badge&message=.NET&color=512BD4&logo=.NET&logoColor=512BD4&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![Chromium](https://img.shields.io/static/v1?style=for-the-badge&message=Chromium&color=006CFF&logo=GoogleChrome&logoColor=006CFF&label=&labelColor=black)](https://github.com/SLT-World/SLBr)<br/>

[![Download](https://img.shields.io/github/downloads/SLT-World/SLBr/total.svg?style=for-the-badge&message=C%23&color=0063FF&label=Downloads&labelColor=0092FF)](https://github.com/SLT-World/SLBr/releases/latest)

</div>

## SLBr
SLBr is an open-source, lightweight web browser based on Chromium. Built with .NET, WPF, and CefSharp (CEF) to provide a modern browsing experience while remaining lightweight.

## Using CefSharp
CefSharp is a .NET wrapper for the Chromium Embedded Framework (CEF), providing an embedded Chromium browser for WPF and WinForms applications. It supports modern web standards, including HTML5, JavaScript, CSS3, WebGL, and HTML5 audio/video.

## Notable Features
See the full feature list, [here](https://slt-world.github.io/slbr/)
- **Clean, Modern UI:** Simple & refreshed design.
- **Ad & Tracker Blocking:** Browse with fewer ads & less tracking.
- **YouTube Ad Skip:** Automatically skips ads on YouTube.
- **Tab Layouts:** Choose vertical or horizontal tab alignment.
- **Tab Unloading:** Save memory by unloading inactive tabs.
- **Smart Address Bar:** Search suggestions directly in the address bar, with quick calculations, weather, and translation.
- **Private Tabs (Incognito Tabs):** Open private browsing sessions that don't store history and cookies.
- **Clipboard & Download Popup:** Attach recent images from the clipboard/downloads, inspired by Opera's Easy Files.
- **Extension Support:** Supports Chrome web store extensions.
- **Google Safe Browsing:** Protects against malicious websites.
- **Anti-Tamper Mode:** Keeps browsing unrestricted by allowing text selection, copy/paste, right-click menus, and developer tools on sites that block them.

## Installation
To install SLBr, follow these steps:
1. Download the [latest release](https://github.com/SLT-World/SLBr/releases/latest).
2. Ensure the following requirements are installed:
    - [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-US/cpp/windows/latest-supported-vc-redist?view=msvc-170) - [Direct Download x64](https://aka.ms/vs/17/release/vc_redist.x64.exe) (Should be bundled in the computer already)
    - [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) - (Launching SLBr without .NET 9.0 will automatically prompt a redirect to a direct download.)
    - [Segoe Fluent Icons](https://learn.microsoft.com/en-us/windows/apps/design/downloads/#fonts) - [Direct Download](https://aka.ms/SegoeFluentIcons) (Windows 11 users are not required to download)
    - Windows 10 & above
## Thanks

- **Chromium Embedded Framework (CEF)**: Thanks to Marshall A. Greenblatt.
- **CefSharp Team**: Thanks to Amaitland and the CefSharp team.
- **IPFS Implementation** (Not present in latest rework): Thanks to Ranger Mauve for assisting with the implementation of IPFS in SLBr.

## Contribution
Feature suggestions and contributions would be much appreciated. Your input helps improve SLBr.
Or you can also contribute by sponsoring [CefSharp](https://github.com/sponsors/amaitland).

## License
SLBr is licensed under the [GNU General Public License v3.0](https://github.com/SLT-World/SLBr/blob/main/LICENSE).

## Screenshots & Videos

![Browser](https://raw.githubusercontent.com/SLT-World/SLBr/main/SLBr/Resources/SLBr%20Browser.png)
![Performance Settings](https://raw.githubusercontent.com/SLT-World/SLBr/main/SLBr/Resources/Performance.png)
![News Feed](https://raw.githubusercontent.com/SLT-World/SLBr/main/SLBr/Resources/News%20Feed.png)

Old Video: [Old SLBr in action](https://youtu.be/PtmDRjgwmHI)

## Others
**Missing class `SECRETS`**

The `SECRETS` file is removed as private API keys are stored inside. To fix it, either:
- Remove the code that is causing the error, which will remove the ability to use Google Safe Browsing & sign in to Google.
- Generate a new C# class called "SECRETS":
```
namespace SLBr
{
    class SECRETS
    {
        public static string GOOGLE_API_KEY = "";
        public static string GOOGLE_DEFAULT_CLIENT_ID = "";
        public static string GOOGLE_DEFAULT_CLIENT_SECRET = "";
        public static string DISCORD_WEBHOOK = "";
    }
}
```
