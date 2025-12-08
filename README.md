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
SLBr is an open-source, lightweight web browser based on Chromium. Built with .NET, WPF, and [CefSharp (CEF) / WebView2] to provide a modern browsing experience while remaining lightweight.

## Notable Features
See the full feature list, [here](https://slt-world.github.io/slbr/)
- **Clean, Modern UI:** Simple & clean design.
- **Multi Web Engine:** Choose between Chromium engine (CEF), Edge engine (WebView2), Internet Explorer engine (Trident).
- **Ad & Tracker Blocking:** Browse with fewer ads & less tracking.
- **Tab Layouts:** Choose vertical or horizontal tab alignment.
- **Tab Unloading:** Save memory by unloading inactive tabs.
- **Smart Address Bar:** Search suggestions directly in the address bar, with quick calculations, weather, and translation.
- **Private Tabs (Incognito Tabs):** Open private browsing sessions that don't store history and cookies.
- **Clipboard & Download Popup:** Attach recent images from the clipboard/downloads, inspired by Opera's Easy Files. (Only available for the Chromium web engine)
- **Extension Support:** Supports Chrome web store extensions.
- **Web Risk Service:** Protects against malicious websites with Google Safe Browsing, Yandex Safe Browsing & PhishTank.
- **Direct Translation:** Directly translate websites without proxies with Google, Microsoft, Yandex & Lingvanex providers.
- **Anti-Tamper Mode:** Keeps browsing unrestricted by allowing text selection, copy/paste, right-click menus, and developer tools on sites that block them.

## Installation
To install SLBr, follow these steps:
1. Download the [latest release](https://github.com/SLT-World/SLBr/releases/latest).
2. Ensure the following requirements are installed:
    - [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-US/cpp/windows/latest-supported-vc-redist?view=msvc-170) - [Direct Download x64](https://aka.ms/vs/17/release/vc_redist.x64.exe) (Should be bundled in the computer already)
    - [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) - (Launching SLBr without .NET 9.0 will automatically prompt a redirect to a direct download.)
    - Windows 10 & above
## Thanks

- **Chromium Embedded Framework (CEF)**: Thanks to Marshall A. Greenblatt.
- **CefSharp Team**: Thanks to Amaitland and the CefSharp team.
- **IPFS Implementation** (Not present in the latest rework): Thanks to Ranger Mauve for assisting with the implementation of IPFS in SLBr.

## License
SLBr is licensed under the [GNU General Public License v3.0](https://github.com/SLT-World/SLBr/blob/main/LICENSE).

## Contribution
Feature suggestions and contributions would be much appreciated. Your input helps improve SLBr.
Or you can also contribute by sponsoring [CefSharp](https://github.com/sponsors/amaitland).

## Screenshots, Videos & Etc

Website: [SLBr](https://slt-world.github.io/slbr/)

New Video: [YouTube](https://www.youtube.com/watch?v=jqx1v6sxK34)

![Browser](https://raw.githubusercontent.com/SLT-World/SLBr/main/Assets/SLBr%20Browser.png)
![Performance Settings](https://raw.githubusercontent.com/SLT-World/SLBr/main/Assets/Performance.png)
![News Feed](https://raw.githubusercontent.com/SLT-World/SLBr/main/Assets/News%20Feed.png)

Old Video: [Old SLBr in action](https://youtu.be/PtmDRjgwmHI)

## Others

> [!IMPORTANT]
> The `SECRETS.cs` file is removed as private API keys are stored inside. To fix it, either:
> - Remove the code that is causing the error, which will remove the ability to use Google Safe Browsing & sign in to Google.
> - Generate a new C# class called "SECRETS":
> ```
> namespace SLBr
> {
>     class SECRETS
>     {
>         public static string GOOGLE_API_KEY = "";
>         public static string GOOGLE_DEFAULT_CLIENT_ID = "";
>         public static string GOOGLE_DEFAULT_CLIENT_SECRET = "";
>         public static string DISCORD_WEBHOOK = "";
>         public static string YANDEX_API_KEY = "";
>         public static string PHISHTANK_API_KEY = "";
>         public static string WEATHER_API_KEY = "";
>         public static string AMP_API_KEY = "";
>         public const string GOOGLE_TRANSLATE_ENDPOINT = "";
>         public const string MICROSOFT_TRANSLATE_ENDPOINT = "";
>         public const string LINGVANEX_ENDPOINT = "";
>         public const string YANDEX_LANGUAGE_DETECTION_ENDPOINT = "";
>         public const string YANDEX_ENDPOINT = "";
>         public const string SPELLCHECK_ENDPOINT = "";
>     }
> }
> ```
