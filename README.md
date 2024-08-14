<div align="center">
  
  # SLBr
  
  **The browser that prioritizes a faster web**<br/>
  **Browse the web with a fast, lightweight web browser.**

[![C#](https://img.shields.io/static/v1?style=for-the-badge&message=C%23&color=239120&logo=csharp&logoColor=239120&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![XAML](https://img.shields.io/static/v1?style=for-the-badge&message=XAML&color=0C54C2&logo=XAML&logoColor=0C54C2&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![.NET](https://img.shields.io/static/v1?style=for-the-badge&message=.NET&color=512BD4&logo=.NET&logoColor=512BD4&label=&labelColor=black)](https://github.com/SLT-World/SLBr)
[![Chromium](https://img.shields.io/static/v1?style=for-the-badge&message=Chromium&color=006CFF&logo=GoogleChrome&logoColor=006CFF&label=&labelColor=black)](https://github.com/SLT-World/SLBr)<br/>

[![Download](https://img.shields.io/github/downloads/SLT-World/SLBr/total.svg?style=for-the-badge&message=C%23&color=0063FF&label=Downloads&labelColor=0092FF)](https://github.com/SLT-World/SLBr/releases/latest)

</div>

## SLBr
SLBr is an open-source, lightweight web browser based on Chromium. Built using .NET, WPF & CefSharp (CEF), SLBr provides a browsing experience on par with modern web browsers.

## Notable Features
- **Modern UI:** A more visually appealing UI compared to older versions.
- **Ad & Tracker Blocker:** Built-in ad & tracker blocking.
- **YouTube Auto Ad Skip:** Automatically skips ads on YouTube.
- **Vertical & Horizontal Tab Alignment:** Align tabs vertically or horizontally in settings.
- **Tab Unloading:** Frees memory and computer resources by unloading inactive tabs.
- **AI Chat and Compose:** Copilot AI chat and composition tools.
- **Omni Box Suggestions:** Suggestions in the omnibox for relevant search results.
- **Enhanced Browser File Explorer:** Better styled Chromium File Explorer.

## Installation
To install SLBr, follow these steps:
1. Download the [latest release](https://github.com/SLT-World/SLBr/releases/latest).
2. Ensure the following requirements are installed:
    - [Microsoft Visual C++ 2019 Redistributable](https://learn.microsoft.com/en-US/cpp/windows/latest-supported-vc-redist?view=msvc-170)
    - [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
    - Segoe Fluent Icons
    - Windows 10 & above
## Thanks

- **Chromium Embedded Framework (CEF)**: Thanks to Marshall A. Greenblatt.
- **CefSharp Team**: Thanks to Amaitland and the CefSharp team.
- **IPFS Implementation**: Thanks to Ranger Mauve for assisting with the implementation of IPFS in SLBr.

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
    }
}
```
